package chat.paramvr.osc

import chat.paramvr.DataType
import chat.paramvr.VrcParametersClient.logger
import chat.paramvr.oscquery.OscQueryServiceData
import com.illposed.osc.*
import chat.paramvr.ws.WebSocketController
import chat.paramvr.oscquery.ServiceDataWatcher
import com.illposed.osc.transport.*
import java.io.IOException
import java.net.InetAddress

object OscController {

    private var portOut: OSCPortOut? = null
    private var portIn: OSCPortIn? = null

    fun close() {
        try {
            portOut?.close()
        } catch (ex: IOException) {
            logger.warn("error closing portOut ${ex.message}")
        }
        try {
            portIn?.close()
        } catch (ex: IOException) {
            logger.warn("error closing portIn ${ex.message}")
        }
    }

    fun init() {
        ServiceDataWatcher.registerServiceDataListener {
            connect(it)
        }
    }

    fun connect(serviceData: OscQueryServiceData) {

        close()
        logger.info("Creating OSC Connection. In = ${serviceData.oscPortIn}, Out = ${serviceData.oscPortOut}")

        portOut = OSCPortOut(InetAddress.getLocalHost(), serviceData.oscPortOut)
        portOut?.connect()

        portIn = OSCPortIn(serviceData.oscPortIn)
        val selector = object : MessageSelector {
            override fun isInfoRequired() = false
            override fun matches(messageEvent: OSCMessageEvent?) = true
        }
        portIn?.dispatcher?.isAlwaysDispatchingImmediately = true
        portIn?.dispatcher?.addListener(selector, OscListener::acceptMessage)
        portIn!!.startListening()
    }

    fun send(param: WebSocketController.VrcParameter) {
        logger.info("Sending param over OSC: ${param.name} = ${param.value}")

        val value = when (param.dataType) {
            DataType.BOOL.id -> param.value.toBoolean()
            DataType.FLOAT.id -> param.value.toFloat()
            DataType.INT.id -> param.value.toInt()
            else -> {
                logger.warn("bad data type ${param.dataType}")
                return
            }
        }

        val msg = OSCMessage("/avatar/parameters/${param.name}", listOf(value))
        portOut!!.send(msg)
    }
}