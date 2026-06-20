# Visor 3D Acadèmic

Aquest projecte és una aplicació interactiva desenvolupada amb **Unity (URP)** pensada per automatitzar el procés de revisió tècnica i visualització de projectes 3D (malles) en entorns acadèmics. Permet carregar models directament al navegador i avaluar-ne el pressupost poligonal.

## Característiques Principals

1. **Drag & Drop a WebGL**: Els alumnes poden arrossegar els seus arxius `.glb` directament sobre la finestra del navegador (sense dependre d'estructures de carpetes locals gràcies a l'API del navegador Web).
2. **Recompte Geomètric Dinàmic**: L'eina llegeix recursivament els `MeshFilter` del model carregat per extreure'n el nombre de vèrtexs i triangles.
3. **Mètrica d'Avaluació Automàtica**: Incorpora un mòdul de regles (`Evaluator.cs`) que assigna una puntuació (de 0.0 a 3.0 pts) segons si el model respecta el límit o pressupost poligonal establert pel docent.
4. **Doble Rol (Professor / Alumne)**: Disposa d'una interfície protegida per contrasenya (`AuthManager.cs`) que permet al docent amagar les dades d'avaluació fins a decidir publicar-les, així com configurar la rúbrica dinàmicament.
5. **Vistes de Diagnòstic**: Permet alternar entre la renderització normal (*Lit*), albedo sense llums (*Unlit*), i malla de filferro (*Wireframe*), tot i que a URP requereix assignar els *shaders* adients manualment per via codi.
6. **Integració amb Ollama**: Inclou un client HTTP asíncron capaç de connectar-se a una instància d'Ollama local (`http://localhost:11434`) per generar un informe qualitatiu basat en les dades analítiques extretes del model.

## Requisits del Sistema

- **Unity Editor**: Versió 2021 LTS, 2022 LTS o superior, amb el mòdul "WebGL Build Support" instal·lat.
- **Render Pipeline**: Universal Render Pipeline (URP).
- **Paquets Addicionals**: 
  - `glTFast` (per llegir models .glb en *runtime*).
  - `TextMeshPro` (per la interfície gràfica).

## Com Configurar el Projecte a l'Escena

Com que els *scripts* es troben desvinculats dels *GameObjects* en aquest repositori base, cal seguir aquests passos dins del Unity Editor:

1. Obre la teva escena o crea'n una de nova.
2. Crea un **GameObject buit** i anomena'l exactament **`ModelLoader`** (el plugin de Javascript el busca per aquest nom).
3. Arrossega a dins de l'objecte `ModelLoader` els següents *scripts* (situats a `Assets/Scripts/`):
   - `ModelLoader.cs`
   - `PolygonCounter.cs`
   - `Evaluator.cs`
   - `RubricConfig.cs`
   - `AuthManager.cs`
   - `OllamaClient.cs`
   - `DiagnosticView.cs`
4. A la finestra *Inspector* de l'objecte `ModelLoader`, arrossega l'objecte mateix sobre tots els camps buits on demani els *scripts* (ex. on demana "Polygon Counter", arrossega el `ModelLoader` sencer, ja que els conté tots).
5. Crea una interfície d'usuari a través d'un *Canvas* amb els menús per al Professor i per a l'Alumne, i enllaça'ls a l'`AuthManager.cs`.

## Compilació a WebGL

Perquè el sistema de *Drag & Drop* funcioni:
1. Vés a `File > Build Settings`.
2. Canvia la plataforma a **WebGL**.
3. Clica a **Build**. Un cop generat, vés a la carpeta i obre l'`index.html` en un navegador web local (pot ser que necessitis un petit servidor local com `python -m http.server` o l'extensió *Live Server* de VSCode per saltar-te les regles de CORS del navegador a l'hora de carregar fitxers).

## Autor
Codi base i documentació generats autònomament pel sistema d'IA Antigravity de Google DeepMind.
