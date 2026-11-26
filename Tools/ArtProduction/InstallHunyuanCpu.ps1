[CmdletBinding()]
param(
    [string]$Runtime = (Join-Path $env:LOCALAPPDATA 'OperationHeist/Hunyuan3D')
)
$ErrorActionPreference = 'Stop'
$upstreamCommit = 'f8db63096c8282cb27354314d896feba5ba6ff8a'
$sourcePath = Join-Path $Runtime 'upstream'
$venvPath = Join-Path $Runtime 'venv'
$pythonPath = Join-Path $venvPath 'Scripts/python.exe'
$null = New-Item -Path $Runtime -ItemType Directory -Force

function Assert-ExternalCommand([string]$Description) {
    if ($LASTEXITCODE -ne 0) { throw "$Description (código $LASTEXITCODE)" }
}

if (!(Test-Path -LiteralPath $sourcePath)) {
    $archive = Join-Path $Runtime "$upstreamCommit.zip"
    curl.exe -fLsS --retry 3 -o $archive "https://codeload.github.com/Tencent-Hunyuan/Hunyuan3D-2/zip/$upstreamCommit"
    Assert-ExternalCommand 'Descarga del código de Tencent'
    Expand-Archive -LiteralPath $archive -DestinationPath $Runtime
    Move-Item -LiteralPath (Join-Path $Runtime "Hunyuan3D-2-$upstreamCommit") -Destination $sourcePath
}
if (Test-Path -LiteralPath (Join-Path $sourcePath '.git')) {
    $actual = git -C $sourcePath rev-parse HEAD
    Assert-ExternalCommand 'Lectura de la revisión de Tencent'
    if ($actual -ne $upstreamCommit) { throw "Código local de Tencent en otra revisión: $actual" }
}

if (!(Test-Path -LiteralPath $pythonPath)) {
    uv venv --python 3.13 $venvPath
    Assert-ExternalCommand 'Creación del entorno Python'
}
uv pip install --python $pythonPath --index-url https://download.pytorch.org/whl/cpu 'torch==2.15.0+cpu' 'torchvision==0.29.2+cpu'
Assert-ExternalCommand 'Instalación de PyTorch CPU'
uv pip install --python $pythonPath 'transformers==4.57.6' 'diffusers==0.35.2' 'accelerate==1.16.0' 'safetensors==0.8.0' 'huggingface-hub==0.36.2' 'pyyaml==6.0.4' 'einops==0.8.2' 'trimesh==5.2.2' 'scikit-image==0.26.0' 'opencv-python-headless==5.1.0.95' 'tqdm==4.70.1' 'omegaconf==2.4.0' 'scipy==1.18.1' 'pymeshlab==2025.7.post1' 'pygltflib==1.16.5'
Assert-ExternalCommand 'Instalación de las dependencias de geometría'
& $pythonPath -c "import sys; sys.path.insert(0, sys.argv[1]); from hy3dgen.shapegen import Hunyuan3DDiTFlowMatchingPipeline; print('HUNYUAN_CPU_RUNTIME_OK')" $sourcePath
Assert-ExternalCommand 'Verificación del entorno'
