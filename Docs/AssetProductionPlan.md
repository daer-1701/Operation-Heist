Actualización posterior: el usuario autorizó modelos nuevos con Hunyuan y Codex. Ver Docs/OriginalModels.md y Docs/GDD-AssetPolicy.md para el estado vigente.

# Plan de producción de assets originales — MVP

Fecha: 29 de octubre de 2026.  
Base: `AGENTS.md`, `Docs/GDD-AssetPolicy.md`.  
Estado: plan documental; **no** hay assets de contenido producidos bajo este plan. No inventa avance de producción ni atribuye autoría humana exclusiva al código asistido ya existente.

## Objetivo y límite de alcance

- Objetivo condicionado: MVP jugable con **dos golpes** (tienda y banco), estilo geométrico propio.
- Presupuesto de referencia original: **240 h**. Ese presupuesto asumía reutilización externa; con contenido 100 % propio **no se garantiza** cerrar dos misiones + red en 240 h.
- Congelados hasta validar dos misiones: **museo** y **capítulos 3–5**.
- Si 240 h no alcanzan: priorizar **primer golpe (tienda)** + **estabilidad** (local/prototipo y red cuando exista stack); banco pasa a alcance condicional.

## Reglas de procedencia

| Permitido (provisional) | Prohibido |
|-------------------------|-----------|
| Unity / URP / Input System / AI Navigation / Test Framework y config técnica | Packs gratuitos o de pago (Kenney, KayKit, Mixamo, Freesound, Asset Store, etc.) |
| Primitivas y composición propia en `04_Models` / prefabs | Texturas, skyboxes, audios, fuentes o prefabs de terceros |
| Materiales de color/parámetros con shaders del motor | Sustituir “gratis” por “comprado” sin autorización |
| UI e iconos vectoriales/simples propios | Fuentes descargadas; packs UI |
| Audio grabado o sintetizado por el equipo, documentado | Silencio sustituido por SFX/música de terceros |
| Animaciones propias sencillas | Rigs/clips Mixamo u otros |

**Pendiente de autorización institucional:** criterio sobre fuentes integradas del motor y sobre arte/audio/animación generados con IA. Hasta confirmación: no importar fuentes de terceros; no incorporar contenido IA; UI final con tipografía integrada solo tras criterio académico.

Cada recurso real se registrará en `Docs/AssetManifest.csv` (origen, método, estado). Un ítem de este plan **no** cuenta como producido.

## Estilo y módulos reutilizables

Biblioteca mínima compartida tienda ↔ banco (primitivas / mallas propias de baja complejidad):

1. Módulo pared / esquina / vano  
2. Suelo / techo / zócalo  
3. Puerta + marco (abierta/cerrada)  
4. Mostrador / mostrador con hueco  
5. Prop bóveda / caja fuerte (caja + puerta)  
6. Props genéricos: caja, estantería baja, poste/barrera  

Materiales: paleta de colores sólidos (suelo, pared, metal, acento alerta, acento botín); sin texturas externas; cámara de prototipo con fondo de color sólido.

UI: canvas simple (HUD, prompts, menú mínimo); iconos geométricos propios; sin fuentes descargadas.

Audio (cuando se produzca): pasos, puerta, alarma corta, UI click; grabación o síntesis propia. Hasta entonces: silencio documentado.

Animación: puertas pivot e indicadores UI; sin headbob ni rigs externos.

## Lista mínima de producción (MVP)

| ID | Recurso | Origen previsto | Prioridad |
|----|---------|-----------------|-----------|
| G01–G06 | Módulos geométricos listados | Composición propia (primitivas Unity) | P0 |
| M01 | Paleta materiales color | Shaders URP / materiales propios | P0 |
| L01 | Layout tienda (primer golpe) | Ensamble de G01–G06 | P0 |
| L02 | Layout banco (segundo golpe) | Reuso G01–G06 + bóveda | P1 |
| U01 | HUD / menú mínimo + iconos | UI Toolkit/uGUI + sprites propios | P0 |
| A01 | Set SFX corto (≤6 clips) | Grabación/síntesis propia | P1 |
| AN01 | Puerta + feedback UI | Animation/Animator propios | P0 |
| — | Museo, caps. 3–5, props narrativos | — | Congelado |

Código de gameplay/red: fuera de este plan de assets; el controlador FPS existente se documenta como **asistido** (Cursor/Codex), no como autoría humana exclusiva.

## Ajuste tentativo a 240 h (contenido + integración visual)

Estimaciones de **planificación**, no de avance medido. Incluyen iteración y registro en manifiesto; excluyen compra/descarga.

| Bloque | Horas orientativas | Notas |
|--------|-------------------|--------|
| Biblioteca módulos + materiales | 40–55 h | Base reutilizable |
| Layout tienda + props mínimos | 35–45 h | Primer golpe |
| Layout banco (reuso) | 25–35 h | Solo si cabe tras P0 |
| UI / iconos | 20–30 h | Tipografía integrada sujeta a autorización |
| Audio propio | 15–25 h | Silencio válido hasta producir |
| Animaciones mínimas | 10–15 h | |
| Manifiesto, auditoría de referencias, buffer | 20–30 h | Cumplimiento de procedencia |
| **Subtotal assets** | **≈165–235 h** | Solapa con gameplay/red fuera de este doc |

**Implicación:** con producción propia, el subtotal de assets puede consumir casi todo el presupuesto de 240 h. Dos golpes completos + red estable no se afirman como entregables garantizados en ese techo.

### Semanas (orientativo, sin cronograma cerrado)

Asumiendo ~20 h/semana de foco en contenido (ajustable al equipo real):

| Semanas | Foco | Criterio de corte |
|---------|------|-------------------|
| 1–2 | G01–G06, M01, manifiesto vacío→primeras filas | Bloques grises reutilizables en editor |
| 3–4 | L01 + U01 + AN01 | Primer golpe navegable visualmente |
| 5 | A01 + pulido tienda | Si desborde: congelar banco |
| 6–7 | L02 solo si P0 estable | Si no: documentar banco como no alcanzado |
| 8 | Auditoría procedencia / buffer | Sin importar terceros “para cerrar” |

Si el ritmo real supera el techo: **dejar banco y audio no críticos**; mantener primer golpe + estabilidad.

## Criterios de aceptación del plan (documental)

- [x] Prohíbe assets gratuitos/terceros y anula fuentes del GDD v2 sustituidas por esta política.  
- [x] Dos golpes como objetivo **condicionado**; museo y caps. 3–5 congelados.  
- [x] Prioridad de recorte: primer golpe + estabilidad si 240 h no bastan.  
- [x] Herramientas de motor permitidas de forma provisional; fuentes integradas e IA pendientes de autorización.  
- [x] No declara assets producidos ni autoría humana exclusiva del código asistido.  

Pendiente fuera de esta tarea: crear/actualizar `AssetManifest.csv` con filas reales, producir módulos, abrir Unity para validar referencias.

## Qué no hace este documento

No descarga, importa ni compra recursos. No genera imágenes IA. No modifica código, escenas, prefabs ni registros de coordinación/tareas.

ORIGINAL_ASSETS_PLAN_READY

