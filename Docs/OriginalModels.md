# Kit original de modelos

Producción: geometría procedural escrita con Codex, sin meshes, texturas ni packs descargados. No se atribuye autoría humana exclusiva. Receta reproducible: Assets/01_Scripts/Editor/OriginalPropsBuilder.cs, métodos Crate, Shelf, Counter y Safe; geometría base propia en FpsPrototypeBuilder. Fecha: 29 de octubre de 2026.

| Modelo | Medida aproximada en metros | Uso |
| --- | --- | --- |
| OH_TransportCrate | 0.84 x 0.62 x 0.62 | Caja de transporte con refuerzos y cierre |
| OH_StorageShelf | 1.60 x 2.00 x 0.52 | Estantería de depósito con cuatro niveles |
| OH_ServiceCounter | 2.40 x 1.05 x 0.82 | Mostrador de tienda o banco |
| OH_CompactSafe | 0.90 x 1.30 x 0.95 | Caja fuerte compacta con volante |

Medidas exactas y triángulos en Docs/QA/OriginalModels-validation.txt tras generar. Unidades: 1 = 1 metro. Raíz al suelo, Y arriba, fachada hacia -Z; mantener escala de raíz en uno.

- Assets/04_Models/Codex: meshes nativos de Unity, regenerables desde código.
- Assets/03_Materials: materiales URP originales de acero verde, metal oscuro y latón, sin texturas externas.
- Assets/02_Prefabs/Props: cuatro prefabs con renderer y BoxCollider.
- Assets/00_Scenes/FPSPrototype.unity: ejemplos integrados para probar con el controlador FPS.
- Docs/AssetManifest.csv: procedencia por archivo; Tools/Audit-Assets.ps1 verifica cobertura.

Operation Heist → Crear kit original e integrar regenera los modelos y reemplaza la composición de FPSPrototype. Guardar cambios propios aparte antes de usarlo. Conserva GUID de meshes existentes y materiales ya creados.

Son props estáticos de prototipo: la caja fuerte no se abre, la caja no se recoge y el collider de estantería ocupa su volumen exterior. No son sistemas de interacción terminados. Hunyuan queda pendiente; prompt en Docs/HunyuanPrompt.md.

## Verificación

Compilación y generación correctas en Unity 6000.5.6f1. Seis pruebas Play Mode aprobadas: cinco del controlador y una de carga de los cuatro modelos en FPSPrototype, meshes, colliders y materiales sin texturas. Evidencia: Docs/QA/OriginalModels-PlayMode.xml. El inventario cubre 32 archivos importables; auditoría sin archivos pendientes de registrar.

Vista general generada y revisada en Docs/QA/OriginalModels-preview.png. Para renderizar en Batch Mode, la prueba inicializa el pipeline configurado mediante un helper de reflexión específico de esta versión de Unity; actualizarlo si se cambia el editor. Este helper solo pertenece a pruebas, no al gameplay. Los archivos de entrega se compararon por hash con los de la copia validada. No se realizó todavía una build Windows ni una evaluación artística final.
