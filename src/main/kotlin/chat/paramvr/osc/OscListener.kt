package chat.paramvr.osc

import chat.paramvr.VrcParametersClient.logger
import chat.paramvr.tray.Manage
import com.illposed.osc.OSCMessageEvent
import chat.paramvr.ws.UpdateQueue

object OscListener {

    // A change in any of these params constitutes activity indicating the user isn't e.g. sleeping.
    // TODO should we have a threshold for small levels of activity that should be considered minor?
    private val activityParams = listOf("Angular", "Velocity", "GestureRight", "GestureLeft")

    // Don't log changes in these params.
    private val suppressParams = listOf("GestureRight", "GestureLeft", "Voice", "Viseme", "Grounded", "Upright")

    // Don't send these params to the server.
    // Upright is sent constantly when in VR, even if there is no motion.
    private val ignoreParams = listOf("InStation", "Seated", "Grounded", "Voice", "Viseme", "TrackingType", "Upright")

    // Do not need to repeat values already present in activityParams or ignoreParams.
    private val doNotCapture = listOf("AFK", "MuteSelf", "VRMode")

    private var lastMovementUpdate: Long = -1

    fun acceptMessage(evt: OSCMessageEvent) {
        val addr = evt.message.address
        val isActivity = isActive(addr)
        if (!isActivity && shouldLog(addr)) {
            logger.info("Received OSC message $addr = ${evt.message.arguments}")
        }

        if (isActivity) {
            val time = System.currentTimeMillis()
            if (time - lastMovementUpdate > 60000) {
                UpdateQueue.enqueue("/chat/paramvr/lastActivity", time)
                lastMovementUpdate = time
            }
        } else if (!shouldIgnore(addr) && (addr.startsWith("/avatar/parameters/") || addr == "/avatar/change")) {

            val value = evt.message.arguments[0]
            UpdateQueue.enqueue(addr, value)
            checkCapture(addr, value)
        }
    }

    private fun checkCapture(addr: String, value: Any) {
        if (addr != "/avatar/change" && !doNotCapture.any{ it == addr }) {
            Manage.capture(addr, value)
        }
    }

    private fun isActive(addr: String) = activityParams.any { addr.startsWith("/avatar/parameters/$it") }

    private fun shouldIgnore(addr: String) = ignoreParams.any { addr.startsWith("/avatar/parameters/$it") }

    private fun shouldLog(addr: String) = !suppressParams.any { addr.startsWith("/avatar/parameters/$it") }
}