"""Generate a solid original campaign prop with the local Hunyuan shape pipeline."""
import argparse
import json
import os
from pathlib import Path
import sys
import time

import numpy as np
from PIL import Image, ImageDraw
import torch


def make_reference(destination: Path, kind: str) -> None:
    image = Image.new("RGBA", (768, 768))
    draw = ImageDraw.Draw(image)
    if kind == "MuseumSignalIdol":
        draw.polygon([(220, 590), (403, 545), (545, 605), (368, 657)], fill="#bd8950")
        draw.polygon([(220, 590), (368, 657), (368, 710), (220, 639)], fill="#916340")
        draw.polygon([(368, 657), (545, 605), (545, 655), (368, 710)], fill="#624137")
        draw.polygon([(320, 600), (302, 497), (345, 444), (442, 475), (456, 568), (386, 627)], fill="#b3864e")
        draw.polygon([(386, 627), (398, 517), (442, 475), (456, 568)], fill="#714638")
        draw.polygon([(305, 522), (222, 390), (253, 266), (380, 172), (507, 222), (540, 369),
                      (447, 519), (387, 559)], fill="#319183")
        draw.polygon([(380, 172), (507, 222), (540, 369), (447, 519), (387, 559), (436, 351)], fill="#235d60")
        draw.polygon([(253, 266), (378, 217), (436, 351), (387, 559), (292, 454)], fill="#4eb4a1")
        draw.polygon([(302, 314), (371, 281), (411, 334), (369, 385), (314, 374)], fill="#e1b66a")
        draw.polygon([(320, 327), (368, 307), (392, 336), (366, 363), (329, 355)], fill="#273e49")
    elif kind == "FinalArchiveCore":
        draw.polygon([(187, 435), (194, 314), (286, 202), (541, 231), (595, 328), (564, 493),
                      (475, 569), (243, 535)], fill="#546a75")
        draw.polygon([(194, 314), (286, 202), (541, 231), (467, 328)], fill="#96a6a8")
        draw.polygon([(194, 314), (467, 328), (475, 569), (243, 535), (187, 435)], fill="#354e5b")
        draw.polygon([(467, 328), (541, 231), (595, 328), (564, 493), (475, 569)], fill="#223847")
        draw.polygon([(232, 351), (436, 365), (440, 463), (243, 445)], fill="#ad7649")
        draw.polygon([(266, 371), (404, 382), (405, 433), (270, 420)], fill="#62c0ad")
        for x in (308, 364, 420, 476):
            draw.line([(x, 218), (x - 54, 294)], fill="#556870", width=11)
    else:
        raise ValueError(kind)
    image.resize((512, 512), Image.Resampling.LANCZOS).save(destination)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--resolution", default=64, type=int)
    parser.add_argument("--seed", default=24112026, type=int)
    parser.add_argument("--steps", default=5, type=int)
    parser.add_argument("--weights", type=Path, default=Path.home() / "Downloads" / "Hunyuan3D-2mini-Turbo")
    parser.add_argument("--runtime", type=Path, default=Path(os.environ["LOCALAPPDATA"]) / "OperationHeist" / "Hunyuan3D")
    parser.add_argument("--image", type=Path, help="Imagen original RGBA de un objeto, con fondo transparente")
    parser.add_argument("models", nargs="*")
    args = parser.parse_args()
    if not args.models or (args.image is not None and len(args.models) != 1):
        parser.error("Indica nombres de modelos o un solo nombre si pasas --image.")
    if args.image is None and set(args.models) - {"MuseumSignalIdol", "FinalArchiveCore"}:
        parser.error("Indica --image para un modelo que no esté en las recetas incluidas.")
    if any(Path(name).name != name or name in {".", ".."} for name in args.models):
        parser.error("Los nombres de modelos deben ser nombres de archivo simples.")
    start = time.monotonic()
    args.out.mkdir(parents=True, exist_ok=True)
    sys.path.insert(0, str(args.runtime / "upstream"))
    os.environ["HF_HUB_OFFLINE"] = "1"
    torch.set_num_threads(6)
    torch.set_num_interop_threads(1)
    from hy3dgen.shapegen import Hunyuan3DDiTFlowMatchingPipeline
    pipeline = Hunyuan3DDiTFlowMatchingPipeline.from_pretrained(
        str(args.weights), subfolder="hunyuan3d-dit-v2-mini-turbo",
        device="cpu", dtype=torch.float32, use_safetensors=True,
    )
    print(f"CPU_MODEL_LOADED {time.monotonic() - start:.1f}s", flush=True)
    for kind in args.models:
        reference = args.out / f"{kind}-reference.png"
        if args.image is None:
            make_reference(reference, kind)
        else:
            reference = args.image
        start = time.monotonic()
        with Image.open(reference) as image:
            mesh = pipeline(
                image=image.convert("RGBA"), num_inference_steps=args.steps, octree_resolution=args.resolution,
                num_chunks=2048, generator=torch.Generator(device="cpu").manual_seed(args.seed),
                output_type="trimesh",
            )[0]
        if mesh is None or len(mesh.faces) == 0 or not np.isfinite(mesh.vertices).all():
            raise RuntimeError(f"Exportación vacía o inválida: {kind}")
        output = args.out / f"{kind}.glb"
        mesh.export(output)
        parts = mesh.split(only_watertight=False)
        mesh = max(parts, key=lambda part: len(part.faces))
        mesh.fix_normals()
        mesh.export(args.out / f"{kind}.obj", include_texture=False)
        report = {
            "model": kind, "device": "CPU", "dtype": "float32", "steps": args.steps,
            "resolution": args.resolution, "seed": args.seed,
            "seconds": round(time.monotonic() - start, 2),
            "vertices": len(mesh.vertices), "triangles": len(mesh.faces),
            "components_before_cleanup": len(parts), "watertight": bool(mesh.is_watertight),
            "bounds": mesh.bounds.tolist(),
        }
        (args.out / f"{kind}.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
        print("CPU_EXPORT_OK", json.dumps(report), flush=True)


if __name__ == "__main__":
    main()
