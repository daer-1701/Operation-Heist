# Arte original de campaña

## Modelos y montaje

Las recetas de [CampaignArtRecipes.json](../../../Tools/ArtProduction/CampaignArtRecipes.json) describen cada modelo y su función. Los prefabs de arte se montan bajo el objeto de interacción que administra el contrato correspondiente. Las unidades son metros, el frente es −Z y el origen de cada conjunto apoya sobre el piso o sobre la superficie de montaje.

| Contrato | Recursos reutilizables | Montaje del lote |
| --- | --- | --- |
| Primer trabajo | Estanterías, cajas, puertas, lector, expediente y terminal del kit | Estación visual del tutorial al inicio. Señal de acceso en el muro. Marcador compartido junto a la zona de extracción. |
| Banco central | Mostrador, ATM, puerta articulada, paneles, barra de oro, efectivo, bolsa y expediente | Lector del acceso alternativo y dos piezas nuevas de botín. Mantener separado el ID de cada pieza que evalúa el contrato. |
| Sombras del museo | Arquitectura, mobiliario, cámara, luces y elementos de servicio del kit | Tabiques de galería de 3 m. La vitrina encaja en incrementos de 1,20 m. Colocar el ídolo y la tablilla en anclajes Exhibit de vitrinas o pedestales separados. |
| Blackout | Muro estándar, escritorios, armarios, cámaras y luminarias | Separadores de oficina de 1,50 m y racks de 0,70 m. Usar Contact para terminal, tapa del armario y cartucho ficticio. Intercambiar el indicador normal y el de emergencia en el mismo Mount. |
| El golpe perfecto | Piso, techo, cámaras, luces y marcador de extracción | Paño subterráneo de 3 m. La esclusa tiene marco fijo y hoja Door. El núcleo se coloca en Exhibit de su pedestal antes de la recogida. |
| Base y cuatro roles | Escritorio, armario, botiquín, linterna y personajes articulados | Mesa de preparación y herramientas de Hacker, Infiltrador, Especialista y Conductor. Grip queda disponible para la mano o el socket del equipo. La variante de refuerzo conserva las articulaciones del guardia. |

Las piezas `Body` son estáticas en el prefab visual. `Door` abre sobre Y, `Lid` sobre X y `Swivel` orienta el cabezal de la cámara sobre Y. Para bloquear, usar cajas simples sobre jambas y dintel y una caja hija de la hoja, con la rotación administrada por el componente de gameplay. Para terminales y controles, montar una caja de alcance frente a `Contact`. Para botín, usar el volumen principal del objeto y el socket de recogida. `SnapLeft` y `SnapRight` son extremos de un módulo y `Mount` es su referencia de montaje. La vitrina tiene vidrio URP transparente y un collider de vidrio puede bloquear el alcance si el contrato exige abrirla antes.

## Generación local

En Windows se usa [InstallHunyuanCpu.ps1](../../../Tools/ArtProduction/InstallHunyuanCpu.ps1) con Python 3.13 y PyTorch CPU. El entorno vive en `%LOCALAPPDATA%/OperationHeist/Hunyuan3D`; los pesos oficiales `safetensors` viven en la carpeta de Descargas de Hunyuan3D-2mini-Turbo. Las referencias de entrada se dibujan con [GenerateHunyuanCpu.py](../../../Tools/ArtProduction/GenerateHunyuanCpu.py) y la salida GLB se convierte en OBJ con [PrepareUnityObj.py](../../../Tools/ArtProduction/PrepareUnityObj.py). El OBJ conserva el componente principal y las normales y obtiene UV0 por proyección de caras. Los prefabs usan los materiales planos URP del proyecto. Esas UV pueden solaparse, por lo que los modelos con texturas horneadas necesitarían un atlas específico.

```powershell
$python = Join-Path $env:LOCALAPPDATA 'OperationHeist/Hunyuan3D/venv/Scripts/python.exe'
& $python Tools/ArtProduction/GenerateHunyuanCpu.py --out D:/ae/OH_CampaignArt_20261124/HunyuanPreview --resolution 56 MuseumSignalIdol FinalArchiveCore
```

El script también acepta una imagen original RGBA mediante `--image` y un nombre para la exportación. Los pesos quedan sujetos a la licencia comunitaria de Tencent Hunyuan3D 2.0, incluidas sus restricciones territoriales sobre los resultados generados.

## Revisión del lote anterior

El archivo [originales.tsv](originales.tsv) registra los 12 FBX, sus dimensiones, UV, normales, mallas, materiales y subrecursos. Las láminas de [vista inicial](originales-1.png) y [vista opuesta](originales-reverso-1.png) siguen ese orden. La [auditoría por GUID](guids.tsv) incluye referencias directas y transitivas de escenas y prefabs.

| Fuente o familia | Hallazgo | Resolución de arte |
| --- | --- | --- |
| `OH_LootBag.fbx` | Malla de 49.994 triángulos. El prefab conserva volumen, textura y silueta de bolsa. Las imágenes fuente del PBR miden 4096 × 4096. | Reutilizar `Production/Loot/OH_LootBag.prefab`. |
| `OH_Dossier.fbx` | La vista opuesta muestra una puerta de bóveda con volante y bisagras. La colocación horizontal del prefab integrado camufla el error de origen. | `Priority/OH_DossierArt.prefab` aporta papel, cubierta, lomo y broche en 0,32 × 0,07 × 0,24 m. En el destino, sustituir la parte visual de `Production/Loot/OH_Dossier.prefab` y conservar el objeto raíz, collider, GUID e ID del objetivo. |
| `OH_SecurityDoor.fbx` y `OH_VaultDoor.fbx` | Un renderizador y una malla por FBX, sin separación de hoja y marco. La puerta de seguridad carece de UV0. | Las variantes `Priority/OH_SecurityDoorArt.prefab` y `Priority/OH_VaultDoorArt.prefab` tienen hoja independiente con la bisagra del kit de producción. Sustituir visual estático y visual de `Door` como hijos de sus equivalentes en los respectivos prefabs de producción. |
| `OH_Agent.fbx`, `OH_Guard.fbx` y `OH_BankClerk.fbx` | Pose estática de alrededor de 1,10–1,15 m. Cada FBX tiene una sola malla sin UV0, huesos, piezas animables ni clips. | Usar `Priority/OH_AgentArt.prefab`, `Priority/OH_GuardArt.prefab` y `Priority/OH_BankClerkArt.prefab` en los visuales de personajes. Conservan articulaciones rígidas del kit, añaden detalles de uniforme y atan las botas a las rodillas. El refuerzo usa la misma jerarquía con señal distintiva. |
| `OH_ElectricalPanel.fbx`, `OH_WirePanel.fbx`, `OH_SecurityCamera.fbx` y `OH_ExtractionTerminal.fbx` | Los FBX son mallas únicas, cada una de unos 50.000 triángulos. El cabezal de cámara y la hoja del panel están unidos a su soporte. La procedencia específica de esos cuatro aportes sigue pendiente. | Las versiones Art definen contacto, anclaje y piezas móviles donde corresponda. La cámara ofrece `Swivel/SightOrigin` y el panel eléctrico tiene hoja `Door`. El panel de cables y el terminal presentan controles abstractos legibles. |
| `20261111001229_7f7e96c8.fbx` | Variante de agente con mochila, en pose estática. Tiene 50.000 triángulos, sin UV0, rig ni referencia en escenas o prefabs. La herramienta y receta originales siguen pendientes. | Fuente preservada en su ruta original para comparación. |

Los FBX de origen conservan su archivo y su .meta. Sus materiales de importación se ven neutros en la lámina de fuente. Los materiales PBR asignados a la bolsa y al expediente en producción se ven en la [lámina del kit](produccion-1.png).
