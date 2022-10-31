package chat.paramvr.tray

import java.awt.MenuItem

class Status(private val label: String) {

    val menuItem = MenuItem()
    private var connected = false

    init {
        menuItem.isEnabled = false
        setConnected(false)
    }

    fun isConnected() = connected

    fun setConnected(connected: Boolean) {
        this.connected = connected
        menuItem.label = "$label Status: ${if (connected) "Connected" else "Disconnected"}"
    }
}