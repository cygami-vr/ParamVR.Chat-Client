import org.jetbrains.kotlin.gradle.tasks.KotlinCompile
import com.github.jengelman.gradle.plugins.shadow.tasks.ShadowJar

plugins {
    application
    kotlin("jvm") version "1.9.0"
    id("com.github.johnrengelman.shadow") version "7.1.2"
    id("com.github.jk1.dependency-license-report") version "2.0"
}

group = "chat.paramvr"
version = "0.1"

application {
    mainClass.set("chat.paramvr.VrcParametersClient")
}

repositories {
    mavenCentral()
}

val ktorVersion = "2.2.4"

dependencies {
    implementation("io.ktor:ktor-client-core:$ktorVersion")
    implementation("io.ktor:ktor-client-cio:$ktorVersion")
    implementation("io.ktor:ktor-client-websockets:$ktorVersion")
    implementation("io.ktor:ktor-serialization-gson:$ktorVersion")
    implementation("io.ktor:ktor-client-okhttp:$ktorVersion")
    implementation("ch.qos.logback:logback-classic:1.4.7")
    implementation("com.illposed.osc:javaosc-core:0.8")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-core:1.7.1")
    testImplementation(kotlin("test"))
}

tasks.test {
    useJUnitPlatform()
}

tasks.withType<KotlinCompile> {
    kotlinOptions.jvmTarget = "17"
}

tasks.withType<ShadowJar> {
    archiveFileName.set("ParamVR.Chat-Client.jar")
}

val jar by tasks.getting(Jar::class) {
    manifest {
        attributes["Main-Class"] = "chat.paramvr.VrcParametersClient"
        attributes["provider"] = "ParamVR.Chat"
        attributes["permissions"] = "all-permissions"
        attributes["codebase"] = "https://paramvr.chat/client/"
    }
}