package chat.paramvr.tray

import chat.paramvr.cfg
import chat.paramvr.ws.WebSocketController
import java.awt.*
import javax.swing.JOptionPane
import kotlin.system.exitProcess


object SystemTrayController {

    fun init() {

        val tray = SystemTray.getSystemTray()
        val iconRes = SystemTrayController::class.java.classLoader.getResource("tray.png")
        val icon = Toolkit.getDefaultToolkit().getImage(iconRes)
        val trayIcon = TrayIcon(icon, "ParamVR.Chat Client")
        val popup = PopupMenu()

        popup.add(createConnectMenuItem())
        popup.add(Manage.createMenu())
        popup.add(Browse.createMenu())
        popup.add(Advanced.createMenu())
        popup.add(About.createMenu())
        popup.add(createExitMenuItem())

        trayIcon.popupMenu = popup
        tray.add(trayIcon)
    }

    private fun createConnectMenuItem(): MenuItem {
        val connect = MenuItem("Connect")
        connect.addActionListener {
            val user = JOptionPane.showInputDialog("Please enter your ParamVR.Chat username.")
            cfg.setTargetUser(user)
            val key = JOptionPane.showInputDialog("Please enter the listen key obtained from the ParamVR.Chat website.")
            cfg.setListenKey(key)
            WebSocketController.connect()
        }
        return connect
    }

    private fun createExitMenuItem(): MenuItem {
        val exit = MenuItem("Exit")
        exit.addActionListener { exitProcess(0) }
        return exit
    }
}