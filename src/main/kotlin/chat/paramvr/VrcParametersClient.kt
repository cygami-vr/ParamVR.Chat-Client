package chat.paramvr

import chat.paramvr.osc.OscController
import chat.paramvr.oscquery.OscQueryController
import chat.paramvr.tray.SystemTrayController
import chat.paramvr.ws.WebSocketController
import org.slf4j.Logger
import org.slf4j.LoggerFactory
import java.io.IOException
import java.nio.channels.FileChannel
import java.nio.file.Paths
import java.nio.file.StandardOpenOption
import kotlin.system.exitProcess

val cfg = ClientConfig()
object VrcParametersClient {

    val logger: Logger = LoggerFactory.getLogger("ParamVR.Chat")
    const val PROTOCOL_VERSION = "0.2"

    @JvmStatic
    fun main(args: Array<String>) {

        // To improve stack traces involving coroutines, add the following to VM Options:
        // -Dkotlinx.coroutines.debug -Dkotlinx.coroutines.stacktrace.recovery=true
        Thread.setDefaultUncaughtExceptionHandler { _, e ->
            logger.error("Uncaught exception", e)
        }

        if (args.isNotEmpty()) {
            cfg.setPath(Paths.get(args[0]))
        }

        try {
            val fc = FileChannel.open(Paths.get(AppData.paramVR()).resolve(".lock"), StandardOpenOption.CREATE, StandardOpenOption.WRITE)
            val lock = fc.tryLock()
            if (lock == null) {
                logger.warn("Another instance of the ParamVR.Client is already running; exiting.")
                exitProcess(-1)
                return
            }
        } catch (ex: IOException) {
            logger.warn("Failed to obtain file lock; exiting.")
            exitProcess(-1)
            return
        }

        Runtime.getRuntime().addShutdownHook(Thread { prepareExit() })

        SystemTrayController.init()
        WebSocketController.connect()
        OscQueryController.init()
        OscController.init()
    }

    private fun prepareExit() {
        logger.info("Preparing to exit process.")
        WebSocketController.close()
        OscController.close()
        OscQueryController.stopService()
    }
}