package chat.paramvr.tray

import chat.paramvr.AppData
import java.awt.Desktop
import java.awt.Menu
import java.awt.MenuItem
import java.net.URI
import java.nio.file.Files
import java.nio.file.Paths

object About {

    fun createMenu(): Menu {

        val about = Menu("About")

        val iconCredit = MenuItem("Icon credit - Toggle button icons created by Icon Mela - Flaticon")
        iconCredit.addActionListener {
            Desktop.getDesktop().browse(URI("https://www.flaticon.com/free-icons/toggle-button"))
        }
        about.add(iconCredit)

        val licenses = MenuItem("View license information")
        licenses.addActionListener {
            val licenseBytes = About::class.java.classLoader.getResource("licenses.html").readBytes()
            val licensePath = Paths.get(AppData.paramVR()).resolve("licenses.html")
            Files.write(licensePath, licenseBytes)
            Desktop.getDesktop().browse(licensePath.toUri())
        }
        about.add(licenses)

        return about
    }
}