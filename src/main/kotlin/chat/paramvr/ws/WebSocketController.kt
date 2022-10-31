package chat.paramvr.ws

import chat.paramvr.VrcParametersClient.logger
import io.ktor.client.*
import io.ktor.client.engine.okhttp.*
import io.ktor.client.plugins.*
import io.ktor.client.plugins.websocket.*
import io.ktor.client.request.*
import io.ktor.http.*
import io.ktor.serialization.gson.*
import kotlinx.coroutines.GlobalScope
import kotlinx.coroutines.launch
import chat.paramvr.cfg
import chat.paramvr.http.BasicHttpClient
import chat.paramvr.http.SslSettings
import chat.paramvr.tray.Advanced
import chat.paramvr.ws.WebSocketHandler.handleMessages
import java.nio.charset.StandardCharsets
import java.util.*

object WebSocketController {

    data class VrcParameter(val name: String, val value: String, val dataType: Short)

    var webSocket: DefaultClientWebSocketSession? = null

    private var client : HttpClient? = null

    fun close() {
        client?.close()
    }

    private fun initClient(): Boolean {
        if (Advanced.vrcpWsStatus.isConnected()) {
            close()
        }

        val cfgHost = cfg.getHost()
        val useSsl = cfgHost != "localhost" && cfgHost != "127.0.0.1"

        client = HttpClient(OkHttp) {
            install(WebSockets) {
                contentConverter = GsonWebsocketContentConverter()
            }
            install(HttpTimeout) {
                requestTimeoutMillis = Long.MAX_VALUE
            }
            if (useSsl) {
                engine {
                    config {
                        sslSocketFactory(SslSettings.getSslContext()!!.socketFactory, SslSettings.getTrustManager())
                    }
                }
            }
        }
        return useSsl
    }

    fun connect() {
        val useSsl = initClient()
        GlobalScope.launch {
            launch(useSsl)
        }
    }
    private suspend fun launch(useSsl: Boolean) {

        val targetUser = cfg.getTargetUser()
        val listenKey = cfg.getListenKey()

        logger.info("Connecting to ${cfg.getHost()}:${cfg.getPort()} as $targetUser:$listenKey")

        val reqBuilder = { reqBuilder: HttpRequestBuilder ->
            reqBuilder.headers.append("Authorization", BasicHttpClient.getAuthorization())
            reqBuilder.url {
                protocol = if (useSsl) URLProtocol.WSS else URLProtocol.WS
                host = cfg.getHost()
                port = cfg.getPort()
            }
        }

        client!!.webSocket(method = HttpMethod.Get, path = "/parameter-listen", request = reqBuilder) {
            handleMessages()
        }

        Thread.sleep(5000)
        if (!Advanced.vrcpWsStatus.isConnected()) {
            logger.info("Attempting auto reconnect...")
            initClient()
            launch(useSsl)
        }
    }
}