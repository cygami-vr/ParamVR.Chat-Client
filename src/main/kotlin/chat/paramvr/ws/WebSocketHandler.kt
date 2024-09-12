package chat.paramvr.ws

import io.ktor.client.plugins.websocket.*
import io.ktor.websocket.*
import kotlinx.coroutines.channels.ClosedReceiveChannelException
import chat.paramvr.VrcParametersClient
import chat.paramvr.VrcParametersClient.logger
import chat.paramvr.http.OscQueryHttpClient
import chat.paramvr.osc.OscController
import chat.paramvr.tray.Advanced
import java.io.BufferedReader
import java.io.InputStreamReader
import javax.swing.JOptionPane
import kotlin.system.exitProcess

object WebSocketHandler {

    suspend fun DefaultClientWebSocketSession.handleMessages() {
        Advanced.vrcpWsStatus.setConnected(true)
        logger.info("WebSocket connected")
        WebSocketController.webSocket = this

        val requiredProtocol = (incoming.receive() as Frame.Text).readText()
        logger.info("Required protocol version = $requiredProtocol")
        // validation is done by the server; can't trust the client
        send(Frame.Text(VrcParametersClient.PROTOCOL_VERSION))
        if (requiredProtocol != VrcParametersClient.PROTOCOL_VERSION) {
            JOptionPane.showMessageDialog(null, "Your ParamVR.Chat-Client is out of date.")
            OscController.close()
            exitProcess(0)
        }

        var avatarId = OscQueryHttpClient.getAvatarId() ?: ""
        logger.info("Sending avatar = $avatarId")
        send(Frame.Text(avatarId))

        sendVRChatStatus()

        try {
            while (true) {
                receiveParameter()
            }
        } catch (ex: ClosedReceiveChannelException) {

            val reconnectExpected = WebSocketController.webSocket !== this
            logger.warn(ex.message)
            logger.warn("Did you just reconnect? I'm guessing ${if (reconnectExpected) "yes" else "no"}")

            if (WebSocketController.webSocket === this) {
                Advanced.vrcpWsStatus.setConnected(false)
            }
        } catch (ex: Exception) {
            logger.error("Error receiving parameter from websocket", ex)
            Advanced.vrcpWsStatus.setConnected(false)
            close()
        }
    }

    private suspend fun DefaultClientWebSocketSession.receiveParameter() {
        val param = receiveDeserialized<WebSocketController.VrcParameter>()
        logger.info("Received param over websocket: ${param.name} = ${param.value}")
        if (param.name == "chat-paramvr-activity") {
            sendVRChatStatus()
        } else {
            try {
                OscController.send(param)
            } catch (ex: Exception) {
                // don't let bad data close the websocket
                logger.error("Error sending data across OSC", ex)
            }
        }
    }

    private fun sendVRChatStatus() {

        var isOpen = false
        val tasklist = Runtime.getRuntime().exec("tasklist /fi \"imagename eq vrchat.exe\"")
        BufferedReader(InputStreamReader(tasklist.inputStream)).use {
            var line: String? = it.readLine()
            while (line != null) {

                if (line.startsWith("VRChat.exe")) {
                    isOpen = true
                    break
                }

                line = it.readLine()
            }
        }

        logger.info("VRC Open = $isOpen")
        UpdateQueue.enqueue("/chat/paramvr/vrcOpen", isOpen)
    }
}