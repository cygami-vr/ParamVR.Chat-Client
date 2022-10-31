package chat.paramvr.tray

import chat.paramvr.AppData
import chat.paramvr.cfg
import java.awt.Desktop
import java.awt.Menu
import java.awt.MenuItem
import java.io.File
import java.net.URI

object Browse {

    fun createMenu(): Menu {

        val browse = Menu("Browse...")

        val host = cfg.getHost()
        val protocol = if (host == "localhost" || host == "127.0.0.1") "http" else "https"
        val url = "$protocol://$host:${cfg.getPort()}/"
        browse.add(createBrowseMenuItem("Manage", url))
        browse.add(createBrowseMenuItem("Shareable parameter trigger", "${url}p/${cfg.getTargetUser()}"))
        browse.add(createDesktopMenuItem("VRChat AppData", AppData.vrChat()))
        browse.add(createDesktopMenuItem("ParamVR.Chat AppData", AppData.paramVR()))

        return browse
    }

    private fun createBrowseMenuItem(label: String, uri: String): MenuItem {
        val item = MenuItem(label)
        item.addActionListener {
            Desktop.getDesktop().browse(URI(uri))
        }
        return item
    }

    private fun createDesktopMenuItem(label: String, file: String): MenuItem {
        val item = MenuItem(label)
        item.addActionListener {
            Desktop.getDesktop().open(File(file))
        }
        return item
    }
}