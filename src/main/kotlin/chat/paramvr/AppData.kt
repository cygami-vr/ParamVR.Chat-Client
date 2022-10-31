package chat.paramvr

import chat.paramvr.VrcParametersClient.logger
import java.nio.file.Paths

object AppData {

    private val appdata = System.getenv("APPDATA")

    init {
        logger.info("APPDATA = $appdata")
    }

    fun paramVR() = "$appdata/ParamVR.Chat/"
    fun vrChat() = "$appdata/../LocalLow/VRChat/VRChat/"
    fun startup() = "$appdata/Microsoft/Windows/Start Menu/Programs/Startup"
    fun runningIn() = Paths.get("").toAbsolutePath()
}