package chat.paramvr.tray

import chat.paramvr.AppData
import chat.paramvr.cfg
import chat.paramvr.http.ParamVrHttpClient
import chat.paramvr.osc.OscController
import chat.paramvr.ws.WebSocketController
import java.awt.Menu
import java.awt.MenuItem
import java.nio.file.Files
import java.nio.file.Paths
import javax.swing.JOptionPane

object Advanced {

    // WebSocket connection
    val vrcpWsStatus = Status("ParamVR.Chat")

    fun createMenu(): Menu {

        val advanced = Menu("Advanced")
        advanced.add(createReconnectNowMenuItem())
        advanced.add(createUploadLogsMenuItem())
        advanced.add(createResetConfigurationMenuItem())
        advanced.add(vrcpWsStatus.menuItem)
        advanced.add(createRunningInMenuItem())
        advanced.add(createSetServerMenuItem())
        advanced.add(createSetOscPortsMenuItem())
        advanced.add(createSetKeyStoreMenuItem())

        return advanced
    }

    private fun createReconnectNowMenuItem(): MenuItem {
        val reconnectNow = MenuItem("Reconnect now")
        reconnectNow.addActionListener {
            WebSocketController.connect()
            OscController.connect()
        }
        return reconnectNow
    }

    private fun createUploadLogsMenuItem(): MenuItem {
        val uploadLogs = MenuItem("Upload logs")
        uploadLogs.addActionListener {

            val option = JOptionPane.showOptionDialog(null,
                "Your logs may contain some personal information. Proceed with upload?",
                "Upload logs", JOptionPane.YES_NO_OPTION,
                JOptionPane.QUESTION_MESSAGE, null, null, null)

            if (option == JOptionPane.YES_OPTION) {
                Files.list(Paths.get(AppData.paramVR()).resolve("logs")).forEach {

                    ParamVrHttpClient.submitForm("/client/log", it.fileName.toString(), Files.readAllBytes(it))
                }
                JOptionPane.showMessageDialog(null, "Upload complete")
            }
        }
        return uploadLogs
    }

    private fun createResetConfigurationMenuItem(): MenuItem {
        val resetConfig = MenuItem("Reset configuration")
        resetConfig.addActionListener {

            val option = JOptionPane.showOptionDialog(null,
                "This will revert the client configuration to the default." +
                        " You will need to re-enter your connection information. Proceed with reset?",
                "Reset configuration", JOptionPane.YES_NO_OPTION,
                JOptionPane.QUESTION_MESSAGE, null, null, null)

            if (option == JOptionPane.YES_OPTION) {
                cfg.reset()
            }

        }
        return resetConfig
    }

    private fun createRunningInMenuItem(): MenuItem {
        val runningIn = MenuItem("Running in ${AppData.runningIn()}")
        runningIn.isEnabled = false
        return runningIn
    }

    private fun createSetServerMenuItem(): MenuItem {
        val setServer = MenuItem("Set Server")
        setServer.addActionListener {

            val option = JOptionPane.showOptionDialog(null,
                "Warning: This will override the default ParamVR.Chat server." +
                        " You probably don't want to do this unless you know what you are doing." +
                        " Do you really want to proceed?",
                "Set server", JOptionPane.YES_NO_OPTION,
                JOptionPane.WARNING_MESSAGE, null, null, null)

            if (option == JOptionPane.YES_OPTION) {
                val host = JOptionPane.showInputDialog("Enter host name")
                cfg.setHost(host)
                val port = JOptionPane.showInputDialog("Enter port number")
                cfg.setPort(port)
                ParamVrHttpClient.init()
                WebSocketController.connect()
            }
        }
        return setServer
    }

    private fun createSetOscPortsMenuItem(): MenuItem {
        val setOscPorts = MenuItem("Set OSC Ports")
        setOscPorts.addActionListener {

            val option = JOptionPane.showOptionDialog(null,
                "Warning: This will override the default OSC ports." +
                        " You probably don't want to do this unless you know what you are doing." +
                        " Do you really want to proceed?",
                "Set OSC ports", JOptionPane.YES_NO_OPTION,
                JOptionPane.WARNING_MESSAGE, null, null, null)

            if (option == JOptionPane.YES_OPTION) {
                val oscInPort = JOptionPane.showInputDialog("Enter OSC in port (default 9001)")
                cfg.setOscInPort(oscInPort)
                val oscOutPort = JOptionPane.showInputDialog("Enter OSC out port (default 9000)")
                cfg.setOscOutPort(oscOutPort)
                OscController.connect()
            }
        }
        return setOscPorts
    }

    private fun createSetKeyStoreMenuItem(): MenuItem {
        val setKeyStore = MenuItem("Set Key Store")
        setKeyStore.addActionListener {

            val option = JOptionPane.showOptionDialog(null,
                "Warning: This will override the default key store." +
                        " You probably don't want to do this unless you know what you are doing." +
                        " Do you really want to proceed?",
                "Set key store", JOptionPane.YES_NO_OPTION,
                JOptionPane.WARNING_MESSAGE, null, null, null)

            if (option == JOptionPane.YES_OPTION) {
                val keyStore = JOptionPane.showInputDialog("Enter key store location")
                cfg.setKeyStoreFile(keyStore)
                val keyStorePasswd = JOptionPane.showInputDialog("Enter key store password")
                cfg.setKeyStorePassword(keyStorePasswd)
                WebSocketController.connect()
            }
        }
        return setKeyStore
    }
}