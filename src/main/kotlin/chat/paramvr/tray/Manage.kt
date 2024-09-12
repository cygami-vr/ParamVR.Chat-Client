package chat.paramvr.tray

import chat.paramvr.AppData
import chat.paramvr.DataType
import chat.paramvr.VrcParametersClient.logger
import chat.paramvr.http.ParamVrHttpClient
import com.google.gson.Gson
import com.google.gson.JsonArray
import com.google.gson.JsonObject
import java.awt.CheckboxMenuItem
import java.awt.Menu
import java.awt.MenuItem
import java.nio.file.Files
import java.nio.file.Path
import java.nio.file.Paths
import java.util.stream.Collectors
import java.util.stream.Stream
import javax.swing.JOptionPane
import kotlin.io.path.exists

object Manage {

    private var capturing = false
    private val captured = mutableMapOf<String, MutableSet<Any>>()
    private val gson = Gson()

    private fun fromJson(p: Path) = gson.fromJson(Files.newBufferedReader(p), JsonObject::class.java)

    fun capture(addr: String, value: Any) {
        if (capturing) {
            logger.info("Captured $addr = $value")

            var values = captured[addr]
            if (values == null) {
                values = mutableSetOf()
                captured[addr] = values
            }
            values += value
        }
    }

    fun createMenu(): Menu {
        val menu = Menu("Manage...")

        menu.add(createRunOnStartupMenuItem())
        menu.add(createImportAvatarsMenuItem())
        menu.add(createCaptureMenuItem())
        menu.add(createEmergencyUnlockMenuItem())

        return menu
    }

    private fun createRunOnStartupMenuItem(): MenuItem {

        val startupFile = Paths.get(AppData.startup()).resolve("ParamVR.Chat.bat")
        val initialState = Files.exists(startupFile)
        logger.info("Run on startup initial state = $initialState")

        val runOnStartup = CheckboxMenuItem("Run on Startup", initialState)
        runOnStartup.addItemListener {
            val on = runOnStartup.state
            logger.info("Toggling run on startup = $on")
            if (on) {
                val absPath = AppData.runningIn().resolve("ParamVR.Chat-Client.jar")
                val cmd = "start javaw -jar \"$absPath\""
                logger.info("Startup cmd = $cmd")
                Files.writeString(startupFile, cmd)
            } else {
                Files.delete(startupFile)
            }
        }
        return runOnStartup
    }

    private fun jsonFiles(): List<JsonObject> {

        val osc = Paths.get(AppData.vrChat()).resolve("OSC")

        return Files.list(osc).flatMap { usr ->
            val avatarsFolder = usr.resolve("Avatars")
            return@flatMap if (avatarsFolder.exists())
                Files.list(avatarsFolder).map { return@map fromJson(it) }
            else
                Stream.empty()
        }.collect(Collectors.toList())
    }

    private fun createImportAvatarsMenuItem(): MenuItem {
        val importAvatars = MenuItem("Import avatars")
        importAvatars.addActionListener {
            jsonFiles().forEach {
                if (!promptImportAvatar(it)) {
                    return@addActionListener
                }
            }
        }
        return importAvatars
    }

    private fun promptImportAvatar(avatar: JsonObject): Boolean {
        val id = avatar.get("id").asString
        val name = avatar.get("name").asString

        val option = JOptionPane.showOptionDialog(null,
            "ID = $id\nName = $name\nImport this avatar?", "Found avatar $name", JOptionPane.YES_NO_CANCEL_OPTION,
            JOptionPane.QUESTION_MESSAGE, null, null, null)

        when (option) {
            JOptionPane.YES_OPTION -> {
                val json = JsonObject()
                json.addProperty("vrcUuid", id)
                json.addProperty("name", name)
                ParamVrHttpClient.post("/client/avatar", json)
            }
            JOptionPane.NO_OPTION -> {
                // nothing to do, just skip
            }
            JOptionPane.CANCEL_OPTION -> {
                return false
            }
        }
        return true
    }

    private fun createCaptureMenuItem(): MenuItem {
        val capture = MenuItem("Start capture")
        capture.addActionListener {
            if (capturing) {
                capturing = false
                capture.label = "Start capture"

                if (captured.isEmpty()) {
                    JOptionPane.showMessageDialog(null, "Nothing was captured; double check that you have OSC enabled.")
                } else {

                    captured.entries.forEach {
                        if (!promptImportParameter(it.key, it.value)) {
                            return@addActionListener
                        }
                    }
                }

            } else {
                captured.clear()
                capture.label = "Stop capture"
                capturing = true
                JOptionPane.showMessageDialog(null,
                    "Capture started. Please change the parameters you would like to capture in VRChat now," +
                            " and then stop the capture when you're done.")
            }
        }
        return capture
    }

    private fun promptImportParameter(name: String, values: Set<Any>): Boolean {
        val option = JOptionPane.showOptionDialog(null,
            "Name = $name\nImport this parameter?", "Found parameter $name", JOptionPane.YES_NO_CANCEL_OPTION,
            JOptionPane.QUESTION_MESSAGE, null, null, null)

        when (option) {
            JOptionPane.YES_OPTION -> {
                val json = JsonObject()
                json.addProperty("name", name.substring(19))
                val dataType = DataType.typeOf(values.first()) ?: return true
                json.addProperty("type", dataType.defaultType())
                json.addProperty("dataType", dataType.id)

                if (dataType == DataType.INT) {
                    val valuesArr = JsonArray()
                    values.forEach {
                        valuesArr.add(it.toString())
                    }
                    json.add("values", valuesArr)
                }

                ParamVrHttpClient.post("/client/parameter", json)
            }
            JOptionPane.NO_OPTION -> {
                // nothing to do, just skip
            }
            JOptionPane.CANCEL_OPTION -> {
                return false
            }
        }
        return true
    }

    private fun createEmergencyUnlockMenuItem(): MenuItem {
        val emergencyUnlock = MenuItem("Emergency unlock")
        emergencyUnlock.addActionListener {
            ParamVrHttpClient.post("client/parameter/emergency-unlock", null)
        }
        return emergencyUnlock
    }
}