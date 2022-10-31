package chat.paramvr

import chat.paramvr.osc.OscController
import chat.paramvr.tray.SystemTrayController
import chat.paramvr.ws.WebSocketController
import org.slf4j.Logger
import org.slf4j.LoggerFactory
import java.nio.file.Paths

val cfg = ClientConfig()

object VrcParametersClient {

    val logger: Logger = LoggerFactory.getLogger("ParamVR.Chat")
    const val PROTOCOL_VERSION = "0.2"

    @JvmStatic
    fun main(args: Array<String>) {

        Thread.setDefaultUncaughtExceptionHandler { _, e ->
            logger.error("Uncaught exception", e)
        }

        if (args.isNotEmpty()) {
            cfg.setPath(Paths.get(args[0]))
        }

        SystemTrayController.init()
        WebSocketController.connect()
        OscController.connect()
    }
}