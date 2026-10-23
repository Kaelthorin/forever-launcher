using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ForeverLauncher;

public static class L
{
	public static readonly string[] Langs = new string[3] { "en", "es", "ru" };

	public static string Lang = "en";

	private static readonly Dictionary<string, string[]> T = new Dictionary<string, string[]>
	{
		{
			"status.checking",
			new string[3] { "CHECKING…", "COMPROBANDO…", "ПРОВЕРКА…" }
		},
		{
			"status.online",
			new string[3] { "ONLINE", "EN LÍNEA", "ОНЛАЙН" }
		},
		{
			"status.worlddown",
			new string[3] { "WORLD UNAVAILABLE", "MUNDO NO DISPONIBLE", "МИР НЕДОСТУПЕН" }
		},
		{
			"status.unreachable",
			new string[3] { "UNREACHABLE FROM YOUR NETWORK", "NO LLEGO DESDE TU RED", "НЕДОСТУПЕН ИЗ ВАШЕЙ СЕТИ" }
		},
		{
			"status.maintenance",
			new string[3] { "MAINTENANCE", "MANTENIMIENTO", "ТЕХНИЧЕСКИЕ РАБОТЫ" }
		},
		{
			"status.offline",
			new string[3] { "OFFLINE", "FUERA DE LÍNEA", "ОФФЛАЙН" }
		},
		{
			"status.noresponse",
			new string[3] { "The server is not responding.", "El servidor no responde.", "Сервер не отвечает." }
		},
		{
			"status.noserver",
			new string[3] { "NO SERVER", "SIN SERVIDOR", "НЕТ СЕРВЕРА" }
		},
		{
			"ui.subtitle",
			new string[3] { "Launcher for the WoW Classic 1.60.1 client on private servers", "Launcher del cliente WoW Classic 1.60.1 para servidores privados", "Лаунчер клиента WoW Classic 1.60.1 для приватных серверов" }
		},
		{
			"ui.news",
			new string[3] { "NEWS", "NOVEDADES", "НОВОСТИ" }
		},
		{
			"ui.login",
			new string[3] { "Login", "Login", "Логин" }
		},
		{
			"ui.world",
			new string[3] { "World", "Mundo", "Мир" }
		},
		{
			"realm.online",
			new string[3] { "Online", "En línea", "Онлайн" }
		},
		{
			"realm.offline",
			new string[3] { "Offline", "Fuera de línea", "Оффлайн" }
		},
		{
			"ui.play",
			new string[3] { "PLAY", "JUGAR", "ИГРАТЬ" }
		},
		{
			"ui.ingame",
			new string[3] { "IN GAME", "EN JUEGO", "В ИГРЕ" }
		},
		{
			"ui.playanother",
			new string[3] { "PLAY ANOTHER", "JUGAR OTRO", "ЕЩЁ ОДНО ОКНО" }
		},
		{
			"ui.changefolder",
			new string[3] { "Change folder", "Cambiar carpeta", "Сменить папку" }
		},
		{
			"ui.gamelang",
			new string[3] { "Game language: {0}", "Idioma del juego: {0}", "Язык игры: {0}" }
		},
		{
			"ui.gamelang.auto",
			new string[3] { "Same as launcher", "Igual que el launcher", "Как в лаунчере" }
		},
		{
			"ui.nofog",
			new string[3] { "Remove fog", "Quitar la niebla", "Убрать туман" }
		},
		{
			"ui.nofogtip",
			new string[3] { "Removes the thick zone fog of Classic (Duskwood, swamps...) while you play. Only on your PC; can be switched while in game.", "Quita la niebla espesa de las zonas de Classic (Bosque del Ocaso, pantanos...) mientras juegas. Solo en tu PC; se puede cambiar en el juego.", "Убирает густой туман зон Classic (Сумеречный лес, болота...) во время игры. Только на вашем ПК; можно переключать прямо в игре." }
		},
		{
			"ui.pickfolder",
			new string[3] { "Choose folder", "Elegir carpeta", "Выбрать папку" }
		},
		{
			"ui.viewlog",
			new string[3] { "View log", "Ver registro", "Открыть лог" }
		},
		{
			"ui.download",
			new string[3] { "Download", "Descargar", "Загрузить" }
		},
		{
			"ui.update",
			new string[3] { "A new launcher version is available (v{0})", "Hay una versión nueva del launcher (v{0})", "Доступна новая версия лаунчера (v{0})" }
		},
		{
			"upd.now",
			new string[3] { "Update now", "Actualizar ahora", "Обновить сейчас" }
		},
		{
			"upd.downloading",
			new string[3] { "Downloading launcher v{0}… {1}%", "Descargando el launcher v{0}… {1}%", "Загрузка лаунчера v{0}… {1}%" }
		},
		{
			"upd.restarting",
			new string[3] { "Restarting the launcher…", "Reiniciando el launcher…", "Перезапуск лаунчера…" }
		},
		{
			"upd.failed",
			new string[3] { "Launcher update v{0} failed: {1}", "Falló la actualización del launcher v{0}: {1}", "Не удалось обновить лаунчер до v{0}: {1}" }
		},
		{
			"upd.nonet",
			new string[3] { "could not download it", "no se pudo descargar", "не удалось загрузить" }
		},
		{
			"upd.done",
			new string[3] { "Launcher updated to v{0}", "Launcher actualizado a v{0}", "Лаунчер обновлён до v{0}" }
		},
		{
			"ui.loading",
			new string[3] { "Loading…", "Cargando…", "Загрузка…" }
		},
		{
			"ui.newsfail",
			new string[3] { "Couldn't load the news.", "No se pudieron cargar las novedades.", "Не удалось загрузить новости." }
		},
		{
			"ui.nonews",
			new string[3] { "No news.", "Sin novedades.", "Новостей нет." }
		},
		{
			"ui.commits",
			new string[3] { "SERVER UPDATES", "CAMBIOS DEL SERVIDOR", "ОБНОВЛЕНИЯ СЕРВЕРА" }
		},
		{
			"ui.nofeed",
			new string[3] { "This server has no news feed (\"statusUrl\" in servers.json).", "Este servidor no tiene novedades (\"statusUrl\" en servers.json).", "У этого сервера нет ленты новостей (\"statusUrl\" в servers.json)." }
		},
		{
			"ui.client",
			new string[3] { "Client {0}", "Cliente {0}", "Клиент {0}" }
		},
		{
			"ui.ready",
			new string[3] { "Ready to play.", "Listo para jugar.", "Готово к игре." }
		},
		{
			"ui.nolog",
			new string[3] { "No log yet (it is created when you play).", "Todavía no hay registro (se crea al jugar).", "Лога пока нет (он создаётся во время игры)." }
		},
		{
			"ui.folderdlg",
			new string[3] { "Choose the _classic_beta_ folder (the one with WowB.exe)", "Elige la carpeta _classic_beta_ (la que tiene WowB.exe)", "Выберите папку _classic_beta_ (в которой лежит WowB.exe)" }
		},
		{
			"ui.server",
			new string[3] { "Server", "Servidor", "Сервер" }
		},
		{
			"ui.editservers",
			new string[3] { "Edit servers", "Editar servidores", "Править серверы" }
		},
		{
			"ui.reloadservers",
			new string[3] { "Reload list", "Recargar lista", "Обновить список" }
		},
		{
			"ui.website",
			new string[3] { "Website", "Web", "Сайт" }
		},
		{
			"srv.none",
			new string[3] { "No server configured: click \"Edit servers\" and add yours to servers.json.", "No hay servidores: pulsa \"Editar servidores\" y añade el tuyo en servers.json.", "Серверов нет: нажмите \"Править серверы\" и добавьте свой в servers.json." }
		},
		{
			"srv.example",
			new string[3] { "Edit servers.json: \"{0}\" still points at the example host.", "Edita servers.json: \"{0}\" todavía apunta al host de ejemplo.", "Отредактируйте servers.json: \"{0}\" всё ещё указывает на тестовый хост." }
		},
		{
			"srv.errors",
			new string[3] { "servers.json has problems: {0}", "servers.json tiene errores: {0}", "В servers.json есть ошибки: {0}" }
		},
		{
			"srv.loaded",
			new string[3] { "{0} server(s) loaded from {1}", "{0} servidor(es) cargados de {1}", "Серверов загружено: {0} (из {1})" }
		},
		{
			"tab.play",
			new string[3] { "PLAY", "JUGAR", "ИГРАТЬ" }
		},
		{
			"tab.local",
			new string[3] { "LOCAL SERVER", "SERVIDOR LOCAL", "ЛОКАЛЬНЫЙ СЕРВЕР" }
		},
		{
			"local.title",
			new string[3] { "Local server: {0}", "Servidor local: {0}", "Локальный сервер: {0}" }
		},
		{
			"local.help",
			new string[3] { "Start and stop a TrinityCore server on this PC for the selected server. Each program opens in its own console window; Stop shuts it down cleanly (Ctrl+C) and only kills it if it doesn't close in time. Leave MySQL empty if it runs as a Windows service.", "Arranca y para un servidor TrinityCore en este PC para el servidor elegido. Cada programa abre su propia consola; Parar lo cierra limpiamente (Ctrl+C) y solo lo mata si no se cierra a tiempo. Deja MySQL vacío si corre como servicio de Windows.", "Запуск и остановка сервера TrinityCore на этом ПК для выбранного сервера. Каждая программа открывается в своём окне консоли; «Остановить» корректно завершает её (Ctrl+C) и принудительно закрывает, только если она не закрылась вовремя. Оставьте поле MySQL пустым, если он работает как служба Windows." }
		},
		{
			"local.bindir",
			new string[3] { "TrinityCore folder (with bnetserver.exe and worldserver.exe)", "Carpeta de TrinityCore (con bnetserver.exe y worldserver.exe)", "Папка TrinityCore (с bnetserver.exe и worldserver.exe)" }
		},
		{
			"local.bindlg",
			new string[3] { "Choose the folder that contains bnetserver.exe and worldserver.exe", "Elige la carpeta que contiene bnetserver.exe y worldserver.exe", "Выберите папку, содержащую bnetserver.exe и worldserver.exe" }
		},
		{
			"local.nobin",
			new string[3] { "That folder doesn't contain bnetserver.exe and worldserver.exe.", "Esa carpeta no tiene bnetserver.exe y worldserver.exe.", "В этой папке нет bnetserver.exe и worldserver.exe." }
		},
		{
			"local.mysqlexe",
			new string[3] { "MySQL server (mysqld.exe, optional)", "Servidor MySQL (mysqld.exe, opcional)", "Сервер MySQL (mysqld.exe, необязательно)" }
		},
		{
			"local.mysqlargs",
			new string[3] { "MySQL arguments (e.g. --console --port=3306 --datadir=\"D:\\TC\\data\")", "Argumentos de MySQL (p. ej. --console --port=3306 --datadir=\"D:\\TC\\data\")", "Аргументы MySQL (напр. --console --port=3306 --datadir=\"D:\\TC\\data\")" }
		},
		{
			"local.browse",
			new string[3] { "Browse…", "Examinar…", "Обзор…" }
		},
		{
			"local.save",
			new string[3] { "Save", "Guardar", "Сохранить" }
		},
		{
			"local.saved",
			new string[3] { "Settings saved.", "Configuración guardada.", "Настройки сохранены." }
		},
		{
			"local.openfolder",
			new string[3] { "Open folder", "Abrir carpeta", "Открыть папку" }
		},
		{
			"local.notset",
			new string[3] { "(not set)", "(sin elegir)", "(не выбрано)" }
		},
		{
			"local.start",
			new string[3] { "Start", "Arrancar", "Запустить" }
		},
		{
			"local.stop",
			new string[3] { "Stop", "Parar", "Остановить" }
		},
		{
			"local.startall",
			new string[3] { "Start all", "Arrancar todo", "Запустить всё" }
		},
		{
			"local.stopall",
			new string[3] { "Stop all", "Parar todo", "Остановить всё" }
		},
		{
			"local.running",
			new string[3] { "Running", "En marcha", "Работает" }
		},
		{
			"local.stopped",
			new string[3] { "Stopped", "Parado", "Остановлен" }
		},
		{
			"local.notconfigured",
			new string[3] { "Not configured", "Sin configurar", "Не настроен" }
		},
		{
			"local.mysqlnone",
			new string[3] { "Not managed (service or remote)", "No gestionado (servicio o remoto)", "Не управляется (служба или удалённый)" }
		},
		{
			"local.starting",
			new string[3] { "Starting {0}…", "Arrancando {0}…", "Запуск {0}…" }
		},
		{
			"local.stopping",
			new string[3] { "Stopping {0}…", "Parando {0}…", "Остановка {0}…" }
		},
		{
			"local.waitmysql",
			new string[3] { "Waiting for MySQL on port {0}…", "Esperando a MySQL en el puerto {0}…", "Ожидание MySQL на порту {0}…" }
		},
		{
			"local.mysqltimeout",
			new string[3] { "MySQL did not open port {0} within 60 s.", "MySQL no abrió el puerto {0} en 60 s.", "MySQL не открыл порт {0} за 60 с." }
		},
		{
			"local.killed",
			new string[3] { "{0} did not close in time and was killed.", "{0} no se cerró a tiempo y se forzó el cierre.", "{0} не закрылся вовремя и был завершён принудительно." }
		},
		{
			"local.noconsole",
			new string[3] { "{0} (PID {1}) has no console (Windows service?): not stopped.", "{0} (PID {1}) no tiene consola (¿servicio de Windows?): no se para.", "У {0} (PID {1}) нет консоли (служба Windows?): не останавливается." }
		},
		{
			"local.done",
			new string[3] { "Done.", "Hecho.", "Готово." }
		},
		{
			"local.realmenabled",
			new string[3] { "Start this realm with \"Start all\"", "Arrancar este reino con \"Arrancar todo\"", "Запустить этот игровой мир можно кнопкой \"Запустить всё\"" }
		},
		{
			"local.setuptitle",
			new string[3] { "First time on this PC? Click Setup", "¿Primera vez en este PC? Pulsa Instalar", "Первый раз на этом ПК? Нажмите «Установка»" }
		},
		{
			"local.setuphelp",
			new string[3] { "Setup extracts the game data from your WoW Classic client, creates this PC's login certificate and your account. It asks for administrator rights once.", "La instalación extrae los datos del juego de tu cliente WoW Classic, crea el certificado de inicio de sesión de este PC y tu cuenta. Pide permisos de administrador una vez.", "Установка извлекает игровые данные из вашего клиента WoW Classic, создаёт сертификат входа для этого ПК и вашу учётную запись. Один раз запросит права администратора." }
		},
		{
			"local.runsetup",
			new string[3] { "Setup", "Instalar", "Установка" }
		},
		{
			"local.clientupdate",
			new string[3] { "Client update", "Actualizar cliente", "Обновление клиента" }
		},
		{
			"local.clientupdatetip",
			new string[3] { "After Blizzard updates the WoW client: lets this server accept the new client build (and run Setup to refresh the game data). Then Stop all and Start all.", "Tras una actualización del cliente de WoW: hace que este servidor acepte la nueva build (y ejecuta Setup para actualizar los datos del juego). Luego Parar todo y Arrancar todo.", "После обновления клиента WoW: сервер начнёт принимать новую сборку (и Setup обновит данные игры). Затем «Остановить всё» и «Запустить всё»." }
		},
		{
			"local.newaccount",
			new string[3] { "Create account", "Crear cuenta", "Создать аккаунт" }
		},
		{
			"local.setupdone",
			new string[3] { "Setup done on this PC.", "Instalación hecha en este PC.", "Установка на этом ПК выполнена." }
		},
		{
			"local.setupneeded",
			new string[3] { "Setup not done yet: click Setup before starting the server.", "Falta la instalación: pulsa Instalar antes de arrancar el servidor.", "Установка ещё не выполнена: нажмите «Установка» перед запуском сервера." }
		},
		{
			"local.setupfirst",
			new string[3] { "Run Setup first (button above): the game data and login certificate are missing.", "Primero pulsa Instalar (botón de arriba): faltan los datos del juego y el certificado.", "Сначала выполните установку (кнопка выше): отсутствуют игровые данные и сертификат входа." }
		},
		{
			"tray.readytitle",
			new string[3] { "Forever Launcher: ready", "Forever Launcher: listo", "Forever Launcher: готово" }
		},
		{
			"tray.readytext",
			new string[3] { "If your first attempt to enter the realm failed, enter again without closing the game.", "Si el primer intento de entrar al reino falló, vuelve a entrar sin cerrar el juego.", "Если первая попытка войти в игровой мир не удалась, войдите снова, не закрывая игру." }
		},
		{
			"tray.open",
			new string[3] { "Open launcher", "Abrir launcher", "Открыть лаунчер" }
		},
		{
			"tray.exit",
			new string[3] { "Exit launcher", "Salir del launcher", "Выйти из лаунчера" }
		},
		{
			"tray.hidden",
			new string[3] { "The launcher stays here while you play. It closes by itself with the game.", "El launcher sigue aquí mientras juegas. Se cierra solo con el juego.", "Лаунчер остаётся здесь, пока вы играете. Он закроется сам вместе с игрой." }
		},
		{
			"exit.confirm",
			new string[3] { "If you close the launcher while playing and the game reconnects, the key won't be applied again and you won't be able to enter the realm.\n\nExit anyway?", "Si cierras el launcher mientras juegas y el juego se reconecta, no se volverá a poner la clave y no podrás entrar al reino.\n\n¿Salir de todas formas?", "Если закрыть лаунчер во время игры и игра переподключится, ключ не будет применён повторно, и вы не сможете войти в игровой мир.\n\nВсё равно выйти?" }
		},
		{
			"app.already",
			new string[3] { "The launcher is already running (check the system tray, next to the clock).", "El launcher ya está abierto (mira en la bandeja del sistema, junto al reloj).", "Лаунчер уже запущен (проверьте системный трей рядом с часами)." }
		},
		{
			"app.crash",
			new string[3] { "The launcher hit an unexpected error and will close.\n\nDetails saved to:\n{0}", "El launcher tuvo un error inesperado y se cerrará.\n\nDetalle guardado en:\n{0}", "В лаунчере произошла непредвиденная ошибка, и он будет закрыт.\n\nПодробности сохранены в:\n{0}" }
		},
		{
			"app.renderfail",
			new string[3] { "Your graphics driver stopped drawing the launcher window, so it will close.\n\nStart it again: from now on it draws the window without the graphics card, which avoids this.\n\nDetails saved to:\n{0}", "El controlador gráfico dejó de dibujar la ventana del launcher, así que se cerrará.\n\nÁbrelo de nuevo: a partir de ahora dibuja la ventana sin la tarjeta gráfica, lo que evita este problema.\n\nDetalle guardado en:\n{0}", "Графический драйвер перестал отрисовывать окно лаунчера, поэтому он будет закрыт.\n\nЗапустите его снова: теперь окно рисуется без видеокарты, и эта ошибка не повторится.\n\nПодробности сохранены в:\n{0}" }
		},
		{
			"dir.choose",
			new string[3] { "Choose the game's _classic_beta_ folder.", "Elige la carpeta _classic_beta_ del juego.", "Выберите папку _classic_beta_ игры." }
		},
		{
			"dir.noexe",
			new string[3] { "WowB.exe is not in that folder (it must be _classic_beta_).", "No encuentro WowB.exe en esa carpeta (tiene que ser _classic_beta_).", "В этой папке нет WowB.exe (нужна папка _classic_beta_)." }
		},
		{
			"dir.untested",
			new string[3] { "Client build {0} is newer than this launcher knows. It should work; if you cannot log in, a launcher update is needed. Playing on the server of this PC (repack)? Press Client update in the LOCAL SERVER tab after every client update, then Stop all and Start all.", "La build {0} del cliente es más nueva de lo que conoce este launcher. Debería funcionar; si no puedes entrar, hace falta actualizar el launcher. ¿Juegas en el servidor de este PC (repack)? Pulsa Actualizar cliente en la pestaña LOCAL SERVER tras cada actualización del cliente y luego Parar todo y Arrancar todo.", "Сборка клиента {0} новее, чем знает этот лаунчер. Должно работать; если не получается войти, нужно обновить лаунчер. Играете на сервере этого ПК (репак)? После каждого обновления клиента нажмите «Обновление клиента» на вкладке LOCAL SERVER, затем «Остановить всё» и «Запустить всё»." }
		},
		{
			"p.nostore",
			new string[3] { "No login key store found in client build {0}: Blizzard probably changed it. A launcher update is needed to log in with this build.", "No se encontró el almacén de claves de inicio de sesión en la build {0}: probablemente Blizzard lo cambió. Hace falta actualizar el launcher para entrar con esta build.", "В сборке клиента {0} не найдено хранилище ключей входа: вероятно, Blizzard его изменила. Чтобы войти с этой сборкой, нужно обновить лаунчер." }
		},
		{
			"p.wtfcreated",
			new string[3] { "Created WTF\\{0} from your Config.wtf", "Creado WTF\\{0} a partir de tu Config.wtf", "Создан WTF\\{0} на основе вашего Config.wtf" }
		},
		{
			"p.wtfupdated",
			new string[3] { "Settings updated: WTF\\{0} (portal {1})", "Configuración actualizada: WTF\\{0} (portal {1})", "Настройки обновлены: WTF\\{0} (портал {1})" }
		},
		{
			"p.nolocale",
			new string[3] { "The game has no {0} text installed, so it opens in English. To get it: Battle.net > WoW Classic > gear icon > Game Settings > Text language.", "El juego no tiene instalado el texto {0}, así que se abre en inglés. Para tenerlo: Battle.net > WoW Classic > engranaje > Configuración del juego > Idioma del texto.", "В игре не установлен текст {0}, поэтому она откроется на английском. Чтобы получить его: Battle.net > WoW Classic > значок шестерёнки > Настройки игры > Язык текста." }
		},
		{
			"p.addon",
			new string[3] { "Installed the {0} addon.", "Instalado el addon {0}.", "Установлен аддон {0}." }
		},
		{
			"p.error",
			new string[3] { "Error: {0}", "Error: {0}", "Ошибка: {0}" }
		},
		{
			"p.multi",
			new string[3] { "Several WowB.exe are running from this folder: close them all and start again.", "Hay varios WowB.exe abiertos desde esta carpeta: ciérralos todos y vuelve a empezar.", "Из этой папки запущено несколько WowB.exe: закройте их все и начните заново." }
		},
		{
			"p.attached",
			new string[3] { "The game was already running: attaching to it.", "El juego ya estaba abierto: me engancho a él.", "Игра уже была запущена: подключаюсь к ней." }
		},
		{
			"p.opening",
			new string[3] { "Opening the game...", "Abriendo el juego...", "Открываю игру..." }
		},
		{
			"p.openinganother",
			new string[3] { "Opening another game window...", "Abriendo otra ventana del juego...", "Открываю ещё одно окно игры..." }
		},
		{
			"p.nostart",
			new string[3] { "The game didn't start within 60 s.", "El juego no arrancó en 60 s.", "Игра не запустилась за 60 с." }
		},
		{
			"p.closed",
			new string[3] { "The game closed. See you next time.", "El juego se cerró. Hasta la próxima.", "Игра закрыта. До встречи." }
		},
		{
			"p.restored",
			new string[3] { "The client restored the original key: applying it again.", "El cliente restauró la clave original: la vuelvo a poner.", "Клиент восстановил исходный ключ: применяю его снова." }
		},
		{
			"p.released",
			new string[3] { "The key store was released; if you reconnect, I'll look for it again.", "El almacén se liberó; si vuelves a conectar, lo buscaré de nuevo.", "Хранилище ключей освобождено; если вы переподключитесь, я найду его снова." }
		},
		{
			"p.already",
			new string[3] { "The key was already applied.", "La clave ya estaba puesta.", "Ключ уже был применён." }
		},
		{
			"p.found",
			new string[3] { "Certificate store found: applying the server key...", "Almacén de certificados encontrado: aplicando la clave del servidor...", "Хранилище сертификатов найдено: применяю ключ сервера..." }
		},
		{
			"p.patchfail",
			new string[3] { "Couldn't patch: {0}. Retrying.", "No se pudo parchear: {0}. Reintento.", "Не удалось применить патч: {0}. Повторяю попытку." }
		},
		{
			"p.many",
			new string[3] { "{0} valid stores at once: waiting until only one remains.", "Hay {0} almacenes válidos a la vez: no escribo hasta que quede uno.", "Одновременно найдено допустимых хранилищ: {0}. Запись отложена, пока не останется одно." }
		},
		{
			"p.support",
			new string[3] { "Help window now opens {0}.", "La ventana de Ayuda ahora abre {0}.", "Окно помощи теперь открывает {0}." }
		},
		{
			"p.waiting",
			new string[3] { "Waiting for you to enter the realm...", "Esperando a que entres al reino...", "Жду, пока вы войдёте в игровой мир..." }
		},
		{
			"p.ready",
			new string[3] { "READY. If your first attempt to enter failed, enter again WITHOUT closing the game.", "LISTO. Si el primer intento de entrar falló, vuelve a entrar SIN cerrar el juego.", "ГОТОВО. Если первая попытка входа не удалась, войдите снова, НЕ закрывая игру." }
		},
		{
			"local.console",
			new string[3] { "Console", "Consola", "Консоль" }
		},
		{
			"local.clearlogs",
			new string[3] { "Clear logs", "Borrar registros", "Очистить логи" }
		},
		{
			"local.clearlogstip",
			new string[3] { "Deletes the log files of the servers that are stopped", "Borra los archivos de registro de los servidores detenidos", "Удаляет файлы логов остановленных серверов" }
		},
		{
			"con.title",
			new string[3] { "Console: {0}", "Consola: {0}", "Консоль: {0}" }
		},
		{
			"con.pause",
			new string[3] { "Pause", "Pausa", "Пауза" }
		},
		{
			"con.clearview",
			new string[3] { "Clear view", "Limpiar vista", "Очистить окно" }
		},
		{
			"con.openlogs",
			new string[3] { "Logs folder", "Carpeta de registros", "Папка логов" }
		},
		{
			"con.filter",
			new string[3] { "Filter", "Filtro", "Фильтр" }
		},
		{
			"con.actions",
			new string[3] { "Quick actions", "Acciones rápidas", "Быстрые действия" }
		},
		{
			"con.send",
			new string[3] { "Send", "Enviar", "Отправить" }
		},
		{
			"con.hint",
			new string[3] { "Log: {0}. Type a GM command (without the dot) and press Enter; Up/Down = history.", "Registro: {0}. Escribe un comando GM (sin el punto) y pulsa Enter; Arriba/Abajo = historial.", "Лог: {0}. Введите команду ГМ (без точки) и нажмите Enter; стрелки вверх/вниз — история." }
		},
		{
			"con.busy",
			new string[3] { "Still waiting for the previous command.", "Aún esperando el comando anterior.", "Ещё жду ответа на предыдущую команду." }
		},
		{
			"con.notready",
			new string[3] { "Commands are not set up yet (see the message above).", "Los comandos aún no están configurados (mira el mensaje de arriba).", "Команды ещё не настроены (см. сообщение выше)." }
		},
		{
			"con.noreply",
			new string[3] { "(no reply)", "(sin respuesta)", "(нет ответа)" }
		},
		{
			"con.sent",
			new string[3] { "Sent: {0}", "Enviado: {0}", "Отправлено: {0}" }
		},
		{
			"con.noconnect",
			new string[3] { "Can't connect to the server's command port {0}. Is this worldserver running (started after turning commands on)?", "No se puede conectar al puerto de comandos {0}. ¿Está en marcha este worldserver (iniciado después de activar los comandos)?", "Не удаётся подключиться к командному порту {0}. Запущен ли этот worldserver (после включения команд)?" }
		},
		{
			"con.loginfailed",
			new string[3] { "The server refused the console account (wrong password or not GM level 3).", "El servidor rechazó la cuenta de consola (contraseña incorrecta o no es GM nivel 3).", "Сервер отклонил консольную учётную запись (неверный пароль или не ГМ 3 уровня)." }
		},
		{
			"con.noconf",
			new string[3] { "Config file not found: {0}", "No se encuentra el archivo de configuración: {0}", "Файл конфигурации не найден: {0}" }
		},
		{
			"con.raoff",
			new string[3] { "Commands are off: the server's Remote Access port is disabled. Turn it on for all realms (this PC only), then restart the worldservers.", "Los comandos están desactivados: el puerto de Acceso Remoto del servidor está apagado. Actívalo para todos los reinos (solo este PC) y reinicia los worldservers.", "Команды выключены: порт удалённого доступа сервера отключён. Включите его для всех миров (только этот ПК), затем перезапустите worldserver'ы." }
		},
		{
			"con.raon",
			new string[3] { "Turn on", "Activar", "Включить" }
		},
		{
			"con.raondone",
			new string[3] { "Remote Access turned on (127.0.0.1, ports 3443+). Restart the worldservers to apply.", "Acceso Remoto activado (127.0.0.1, puertos 3443+). Reinicia los worldservers para aplicarlo.", "Удалённый доступ включён (127.0.0.1, порты 3443+). Перезапустите worldserver'ы." }
		},
		{
			"con.noaccount",
			new string[3] { "No console account yet: the launcher logs in to the server with its own GM account. Create it (MySQL must be running).", "Aún no hay cuenta de consola: el lanzador entra al servidor con su propia cuenta GM. Créala (MySQL debe estar en marcha).", "Консольной учётной записи пока нет: лаунчер входит на сервер со своей учётной записью ГМ. Создайте её (MySQL должен работать)." }
		},
		{
			"con.makeaccount",
			new string[3] { "Create console account", "Crear cuenta de consola", "Создать консольную учётку" }
		},
		{
			"con.creating",
			new string[3] { "Creating the account...", "Creando la cuenta...", "Создаю учётную запись..." }
		},
		{
			"con.accountmade",
			new string[3] { "Console account {0} created and saved.", "Cuenta de consola {0} creada y guardada.", "Консольная учётная запись {0} создана и сохранена." }
		},
		{
			"con.accountdone",
			new string[3] { "Account {0} created (game account {1}, GM level {2}).", "Cuenta {0} creada (cuenta de juego {1}, nivel GM {2}).", "Учётная запись {0} создана (игровая {1}, уровень ГМ {2})." }
		},
		{
			"con.exists",
			new string[3] { "{0} already exists.", "{0} ya existe.", "{0} уже существует." }
		},
		{
			"con.createfailed",
			new string[3] { "The account could not be created (worldserver exit code {0}; is MySQL running?).", "No se pudo crear la cuenta (código de salida {0}; ¿está MySQL en marcha?).", "Не удалось создать учётную запись (код выхода {0}; запущен ли MySQL?)." }
		},
		{
			"con.confirm",
			new string[3] { "Send \"{0}\"?", "¿Enviar \"{0}\"?", "Отправить «{0}»?" }
		},
		{
			"con.ok",
			new string[3] { "OK", "Aceptar", "ОК" }
		},
		{
			"con.cancel",
			new string[3] { "Cancel", "Cancelar", "Отмена" }
		},
		{
			"con.logscleared",
			new string[3] { "Deleted {0} log files ({1} MB).", "Borrados {0} archivos de registro ({1} MB).", "Удалено файлов логов: {0} ({1} МБ)." }
		},
		{
			"con.logsrunning",
			new string[3] { "Skipped (running): {0}.", "Omitidos (en marcha): {0}.", "Пропущены (работают): {0}." }
		},
		{
			"con.a.online",
			new string[3] { "Who's online", "Quién está conectado", "Кто в сети" }
		},
		{
			"con.a.info",
			new string[3] { "Server info", "Información del servidor", "Информация о сервере" }
		},
		{
			"con.a.announce",
			new string[3] { "Announce", "Anuncio", "Объявление" }
		},
		{
			"con.a.money",
			new string[3] { "Give gold", "Dar oro", "Выдать золото" }
		},
		{
			"con.a.item",
			new string[3] { "Give item", "Dar objeto", "Выдать предмет" }
		},
		{
			"con.a.level",
			new string[3] { "Set level", "Cambiar nivel", "Установить уровень" }
		},
		{
			"con.a.tele",
			new string[3] { "Teleport player", "Teletransportar jugador", "Телепортировать игрока" }
		},
		{
			"con.a.revive",
			new string[3] { "Revive player", "Revivir jugador", "Воскресить игрока" }
		},
		{
			"con.a.kick",
			new string[3] { "Kick player", "Expulsar jugador", "Выгнать игрока" }
		},
		{
			"con.a.ban",
			new string[3] { "Ban character", "Banear personaje", "Забанить персонажа" }
		},
		{
			"con.a.unban",
			new string[3] { "Unban character", "Desbanear personaje", "Разбанить персонажа" }
		},
		{
			"con.a.save",
			new string[3] { "Save all", "Guardar todo", "Сохранить всё" }
		},
		{
			"con.a.restart",
			new string[3] { "Restart server", "Reiniciar servidor", "Перезапустить сервер" }
		},
		{
			"con.a.shutdown",
			new string[3] { "Shut down server", "Apagar servidor", "Выключить сервер" }
		},
		{
			"con.a.cancel",
			new string[3] { "Cancel restart/shutdown", "Cancelar reinicio/apagado", "Отменить перезапуск/выключение" }
		},
		{
			"con.a.account",
			new string[3] { "Create account", "Crear cuenta", "Создать учётную запись" }
		},
		{
			"con.f.message",
			new string[3] { "Message", "Mensaje", "Сообщение" }
		},
		{
			"con.f.player",
			new string[3] { "Character name", "Nombre del personaje", "Имя персонажа" }
		},
		{
			"con.f.gold",
			new string[3] { "Gold", "Oro", "Золото" }
		},
		{
			"con.f.itemid",
			new string[3] { "Item ID", "ID del objeto", "ID предмета" }
		},
		{
			"con.f.count",
			new string[3] { "Count", "Cantidad", "Количество" }
		},
		{
			"con.f.level",
			new string[3] { "Level", "Nivel", "Уровень" }
		},
		{
			"con.f.location",
			new string[3] { "Location (.tele name, e.g. goldshire)", "Lugar (nombre de .tele, p. ej. goldshire)", "Место (имя для .tele, напр. goldshire)" }
		},
		{
			"con.f.duration",
			new string[3] { "Duration (e.g. 1h, 1d, -1 = forever)", "Duración (p. ej. 1h, 1d, -1 = para siempre)", "Срок (напр. 1h, 1d, -1 — навсегда)" }
		},
		{
			"con.f.reason",
			new string[3] { "Reason", "Motivo", "Причина" }
		},
		{
			"con.f.seconds",
			new string[3] { "Seconds", "Segundos", "Секунд" }
		},
		{
			"con.f.email",
			new string[3] { "E-mail (login name)", "Correo (nombre de inicio de sesión)", "E-mail (логин)" }
		},
		{
			"con.f.password",
			new string[3] { "Password (4-16 characters)", "Contraseña (4-16 caracteres)", "Пароль (4–16 символов)" }
		},
		{
			"con.f.gmlevel",
			new string[3] { "GM level (0 = player, 3 = admin)", "Nivel GM (0 = jugador, 3 = admin)", "Уровень ГМ (0 — игрок, 3 — админ)" }
		}
	};

	private static string LangFile => Path.Combine(Settings.Dir, "lang.txt");

	public static string Get(string key, params object[] args)
	{
		return GetIn(Lang, key, args);
	}

	public static string GetIn(string lang, string key, params object[] args)
	{
		if (!T.TryGetValue(key, out var value))
		{
			return key;
		}
		int num = Array.IndexOf(Langs, lang);
		string text = value[(num >= 0 && num < value.Length) ? num : 0];
		if (args != null && args.Length != 0)
		{
			return string.Format(text, args);
		}
		return text;
	}

	public static void Load()
	{
		try
		{
			string text = File.ReadAllText(LangFile).Trim();
			if (Array.IndexOf(Langs, text) >= 0)
			{
				Lang = text;
				return;
			}
		}
		catch
		{
		}
		string twoLetterISOLanguageName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
		Lang = ((Array.IndexOf(Langs, twoLetterISOLanguageName) >= 0) ? twoLetterISOLanguageName : "en");
	}

	public static void Save()
	{
		try
		{
			Directory.CreateDirectory(Settings.Dir);
			File.WriteAllText(LangFile, Lang);
		}
		catch
		{
		}
	}
}
