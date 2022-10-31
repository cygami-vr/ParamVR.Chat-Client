import org.jetbrains.kotlin.gradle.tasks.KotlinCompile
import com.github.jengelman.gradle.plugins.shadow.tasks.ShadowJar

plugins {
    application
    kotlin("jvm") version "1.6.21"
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

val ktorVersion = "2.0.2"

dependencies {
    implementation("io.ktor:ktor-client-core:$ktorVersion")
    implementation("io.ktor:ktor-client-cio:$ktorVersion")
    implementation("io.ktor:ktor-client-websockets:$ktorVersion")
    implementation("io.ktor:ktor-serialization-gson:$ktorVersion")
    implementation("io.ktor:ktor-client-okhttp:$ktorVersion")
    implementation("ch.qos.logback:logback-classic:1.2.11")
    implementation("com.illposed.osc:javaosc-core:0.8")
    testImplementation(kotlin("test"))
}

tasks.test {
    useJUnitPlatform()
}

/*tasks.named<JavaExec>("run") {
    standardInput = System.`in`
}*/

tasks.withType<KotlinCompile> {
    kotlinOptions.jvmTarget = "1.8"
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

/*
ant.jar(destfile: it, update: true) {
    delegate.manifest {
        attribute(name: 'permissions', value: 'all-permissions')
        attribute(name: 'codebase', value: '*')
    }
}*/