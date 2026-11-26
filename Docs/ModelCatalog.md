# Modelos originales para depósito y banco

Actualización del 10 de noviembre. Los prefabs `OH_LootBag` y `OH_Dossier` usan las mallas Hunyuan aportadas por el usuario, ajustadas al volumen de los colliders originales. La bolsa tiene 49.994 triángulos y el expediente 50.000. Ambos usan el color y las normales extraídos de los FBX. La tabla inferior conserva las cifras del kit procedural original. El FBX de la puerta nueva está registrado para la siguiente integración. [Procedencia](AssetManifest.csv)

Generados con Codex el 5 de noviembre de 2026. Recetas en Tools/ModelRecipes.json y ProductionModelsBuilder.cs.
Unidades en metros, eje Y arriba, frente hacia -Z, escala de raíz uno. Materiales planos URP originales.
Los personajes usan piezas rígidas articuladas. Las animaciones y la integración con gameplay quedan como trabajo posterior.

| Modelo | Categoría | Tamaño en metros | Triángulos |
| --- | --- | --- | --- |
| OH_Wall | Architecture | (3.00, 3.00, 0.22) | 36 |
| OH_WallHalf | Architecture | (1.50, 3.00, 0.22) | 36 |
| OH_WallCorner | Architecture | (1.50, 3.00, 1.50) | 24 |
| OH_FloorTile | Architecture | (3.00, 0.11, 3.00) | 36 |
| OH_CeilingTile | Architecture | (3.00, 0.26, 3.00) | 24 |
| OH_Column | Architecture | (0.60, 3.00, 0.60) | 36 |
| OH_DoorFrame | Architecture | (1.34, 2.37, 0.25) | 36 |
| OH_SecurityDoor | Architecture | (1.38, 2.37, 0.29) | 144 |
| OH_WindowWall | Architecture | (3.00, 3.00, 0.18) | 96 |
| OH_Stairs | Architecture | (1.50, 1.44, 2.24) | 96 |
| OH_VentDuct | Architecture | (1.50, 0.65, 0.65) | 36 |
| OH_VentGrille | Architecture | (0.94, 0.87, 0.25) | 120 |
| OH_VaultDoor | Bank | (2.54, 2.77, 0.74) | 396 |
| OH_RollingShutter | Architecture | (3.25, 3.25, 0.40) | 216 |
| OH_Pallet | Depot | (1.20, 0.26, 1.03) | 108 |
| OH_CardboardBox | Depot | (0.65, 0.61, 0.51) | 36 |
| OH_ShippingCase | Depot | (0.90, 0.76, 0.66) | 48 |
| OH_WarehouseRack | Depot | (2.47, 2.50, 0.82) | 216 |
| OH_HandTruck | Depot | (0.60, 1.43, 0.69) | 144 |
| OH_Locker | Depot | (0.65, 1.90, 0.60) | 84 |
| OH_Desk | Furniture | (1.50, 0.80, 0.80) | 108 |
| OH_Chair | Furniture | (0.52, 1.13, 0.52) | 72 |
| OH_Bench | Furniture | (1.90, 1.05, 0.60) | 72 |
| OH_TellerCounter | Bank | (2.62, 2.28, 0.52) | 96 |
| OH_ATM | Bank | (0.75, 1.65, 0.58) | 60 |
| OH_DepositBoxes | Bank | (1.80, 2.20, 0.57) | 588 |
| OH_QueuePost | Bank | (0.32, 1.07, 0.32) | 144 |
| OH_QueueBarrier | Bank | (1.82, 1.00, 0.32) | 204 |
| OH_SafeCabinet | Bank | (0.90, 1.30, 0.81) | 84 |
| OH_OfficeCabinet | Furniture | (0.90, 1.40, 0.57) | 60 |
| OH_Monitor | Equipment | (0.55, 0.60, 0.25) | 48 |
| OH_Keyboard | Equipment | (0.45, 0.06, 0.16) | 444 |
| OH_SecurityCamera | Security | (0.22, 0.23, 0.67) | 60 |
| OH_Keypad | Security | (0.20, 0.40, 0.09) | 168 |
| OH_ElectricalPanel | Security | (0.60, 0.90, 0.22) | 48 |
| OH_WirePanel | Security | (0.70, 0.80, 0.15) | 156 |
| OH_AlarmBeacon | Security | (0.30, 0.34, 0.25) | 72 |
| OH_ExtractionTerminal | Security | (0.70, 1.55, 0.45) | 60 |
| OH_CeilingLamp | Furniture | (1.10, 0.14, 0.22) | 24 |
| OH_WallLight | Furniture | (0.20, 0.50, 0.18) | 24 |
| OH_FireExtinguisher | Security | (0.22, 0.68, 0.19) | 132 |
| OH_TrashBin | Furniture | (0.42, 0.66, 0.42) | 96 |
| OH_Planter | Furniture | (0.45, 1.07, 0.45) | 108 |
| OH_Dossier | Loot | (0.32, 0.07, 0.24) | 60 |
| OH_LootBag | Loot | (0.64, 0.54, 0.32) | 668 |
| OH_CashBundle | Loot | (0.16, 0.09, 0.08) | 24 |
| OH_GoldBar | Loot | (0.22, 0.11, 0.10) | 12 |
| OH_AccessCard | Equipment | (0.09, 0.01, 0.06) | 36 |
| OH_Radio | Equipment | (0.09, 0.34, 0.05) | 108 |
| OH_Flashlight | Equipment | (0.10, 0.30, 0.10) | 144 |
| OH_Medkit | Equipment | (0.34, 0.34, 0.16) | 48 |
| OH_ToolCase | Equipment | (0.52, 0.36, 0.21) | 60 |
| OH_Agent | Characters | (0.85, 2.02, 0.31) | 1052 |
| OH_Guard | Characters | (0.85, 2.02, 0.31) | 1052 |
| OH_BankClerk | Characters | (0.85, 2.02, 0.31) | 1064 |
