package chat.paramvr.oscquery

import chat.paramvr.AppData
import chat.paramvr.VrcParametersClient.logger
import com.google.gson.Gson
import java.io.IOException
import java.nio.charset.StandardCharsets
import java.nio.file.*
import kotlin.io.path.isDirectory

object OscQueryController {

    private val oscQueryDir = Paths.get(AppData.paramVR()).resolve("oscquery-service")
    private var oscQueryServiceProcess: Process? = null

    fun init() {
        forceKillAllServices()
        extractService()

        ServiceDataWatcher.startThread()
        startService()
    }

    private fun forceKillAllServices() {

        val taskkill = Runtime.getRuntime().exec("taskkill /F /fi \"imagename eq paramvr.chat-oscqueryservice.exe\"")
        taskkill.inputStream.bufferedReader(StandardCharsets.UTF_8).use {
            var line: String? = it.readLine()
            while (line != null) {
                logger.info("Reading stdout from taskkill: $line")
                line = it.readLine()
            }
        }
        val exitCode = taskkill.waitFor()
        logger.info("taskkill exited with code $exitCode")
    }

    private fun extractService() {
        // Should we use some kind of digest comparison to check if the extraction is necessary?
        Files.createDirectories(oscQueryDir)
        Files.list(oscQueryDir).filter { p -> !p.isDirectory() }.forEach { p -> Files.deleteIfExists(p) }
        val zipName = "ParamVR.Chat-OSCQueryService.zip"
        val zipBytes = OscQueryController::class.java.classLoader.getResource(zipName).readBytes()
        Files.write(oscQueryDir.resolve(zipName), zipBytes)
        val unzipProcess = Runtime.getRuntime().exec("tar -xf $zipName", null, oscQueryDir.toFile())
        unzipProcess.inputStream.bufferedReader(StandardCharsets.UTF_8).use {
            var line: String? = it.readLine()
            while (line != null) {
                logger.info("Reading stdout from tar: $line")
                line = it.readLine()
            }
        }
        val exitCode = unzipProcess.waitFor()
        logger.info("tar unzip exited with code $exitCode")
    }

    private fun startService() {
        oscQueryServiceProcess = Runtime.getRuntime().exec(oscQueryDir.resolve("ParamVR.Chat-OSCQueryService.exe").toString())
        logger.info("ParamVR.Chat-OSCQueryService started with PID ${oscQueryServiceProcess?.pid()}")
    }

    fun stopService() {
        try {
            oscQueryServiceProcess?.let {
                it.outputStream.bufferedWriter(StandardCharsets.UTF_8).use { writer ->
                    writer.write("Exit")
                    writer.flush()
                }
            }
        } catch (ex: IOException) {
            logger.warn("Error closing the OSCQuery service", ex)
        }
    }
}