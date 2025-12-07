# Artes Arcanas – Servidor (.NET 8)

Este directorio contiene la nueva solución orientada a migrar el servidor clásico escrito en Pascal a .NET 8. La idea es mantener compatibilidad binaria con el cliente legado (Pascal y futuro MonoGame) reutilizando los mismos archivos (`opciones.txt`, `avatares/*.avt`, `clanes.dat`, etc.).

## Estructura

| Proyecto | Descripción |
| --- | --- |
| `ArtesArcanas.Server.Core` | Biblioteca que contiene la lógica principal (configuración, estructuras legacy, registro de administradores, logger, etc.). |
| `ArtesArcanas.Server.App` | Ejecutable de consola que hospeda el servidor y expone algunos parámetros (`--root`, `--legacy-root`, `--options`). |

La solución referencia `MonoGameClient/src/Laa.Content.Core` para reutilizar los descriptores de contenido existentes.

## Ejecución

```bash
cd Servidor
dotnet run --project src/ArtesArcanas.Server.App
```

Parámetros opcionales:

| Opción | Descripción |
| --- | --- |
| `--root <ruta>` | Sobrescribe el directorio base del servidor (por defecto es el directorio actual). |
| `--legacy-root <ruta>` | Carpeta donde se buscan `opciones.txt`, `avatares`, `clanes.dat`, etc. Por defecto apunta a `../Original Pascal/Servidor`. |
| `--options <ruta>` | Ruta explícita para un archivo `opciones.txt`. |

Presiona `Ctrl+C` para detener la aplicación.

## Estado actual

* Se parsea el `opciones.txt` original (mensajes, puertos, banderas, posiciones base, etc.).
* Se implementaron lectores binarios compatibles para `admin.dat` y `avatares/*.avt`.
* El host carga la configuración, reserva slots al estilo del servidor Pascal y ahora expone un `TcpListener` en el puerto configurado. El challenge `'|'+B2aStr+B4aStr` está implementado y valida la respuesta/versión del cliente.
* El comando `'!'` de inicio de sesión se procesa: se leen los 32 bytes del hash SHA-256, el tamaño del login y el identificador. Se validan versiones, baneos y contraseñas comparando directamente contra el hash almacenado en `avatares/*.avt`.
* Tras autenticar, se construye la carga `'@' + flags + ExtraerDatosEnCadena` directamente desde el snapshot del avatar (se portó la lógica de `InventarioACadena`, `getHabilidades`, etc.). Luego se envía `'!'`, se muestran los mensajes de bienvenida y la sesión queda abierta.
* Cuando un avatar aparece o abandona el servidor se generan los paquetes legacy `N` y `~` dirigidos únicamente a los jugadores del mismo mapa, y tanto el chat local (`H`) como los paquetes de movimiento `p/*P/r` dejaron de difundirse globalmente para replicar el comportamiento original de `EnviarAlMapa_J`.
* El bucle del mundo ahora mantiene una grilla de ocupación por mapa: durante el login se reserva una casilla libre (buscando posiciones cercanas si la original está ocupada) y los movimientos `m/M` sólo prosperan si la nueva casilla está libre, evitando que varios avatares compartan el mismo tile.
* El intérprete ahora entiende los comandos de inventario y bolsas (`c`, `u`, `F`, `S`, `R`, `r`, `a`, `$`): las órdenes se procesan en el loop, manipulan directamente los slots del snapshot, generan paquetes de refresco (`216+slot`) y registran bolsas temporales por mapa/coordenada mediante un `LegacyGroundItemStore` mientras se termina de portar la lógica completa de objetos/dinero.
* Durante el arranque se deserializa el catálogo `items.json` (extraído del `obj.b` original), exponiendo nombre, flags y cantidades máximas de cada objeto. El `LegacyWorldActionSink` usa esa tabla para describir qué se consume/usa, validar apilados y mantener los modificadores correctos al dividir un stack.
* El comando `'R'` vuelve a responder con el paquete legacy `IO` (30 pares `id/modificador`), lo que permite al cliente original poblar la ventana de bolsas. Las operaciones `S/r/a` ahora respetan `cantidad=0` como “todo el stack”, reinsertan los remanentes en la misma bolsa y registran mensajes con el nombre real del objeto. Las bolsas especiales (`Lenna`, fogatas, trampas) restauran sus marcadores originales (`#195/#196/#203`) y ya no se permite soltar objetos sobre trampas activas.
* Al crear, actualizar o eliminar bolsas, el servidor vuelve a emitir los mismos eventos visuales del servidor Pascal (`#194` para colocar la bolsa en el mapa, `#192/#193` para retirarla) y mantiene una lista de jugadores que están “revisando” la bolsa para reenviarles automáticamente los paquetes `IO` cuando alguien deposita o levanta objetos.
* Se añadió un intérprete básico para los comandos `'H'` (chat local), `'T'` (global, condicionado a las opciones) y `X!` (salir). El resto de opcodes ya se enrutan a un `LegacyCommandRouter`/`LegacyCommandDispatcher`, que mantienen una cola asincrónica y registran cada comando para facilitar el port del loop de juego. El chat local se difunde solamente a los jugadores del mismo mapa mientras que el global sigue respetando las banderas del `opciones.txt`.

> **Nota:** `dotnet build` intentará restaurar paquetes desde `nuget.org`. En el entorno restringido actual la petición HTTPS es rechazada, por lo que la compilación falla con `Unable to load the service index for source https://api.nuget.org/v3/index.json`. Ejecuta `dotnet build --ignore-failed-sources` o configura un feed local si necesitas validar la compilación fuera de este entorno.
