"""Prepare generated solids for Unity. Bake a face projection for each OBJ UV corner."""
import argparse
from pathlib import Path

import numpy as np
import pymeshlab
import trimesh


def prepare(source: Path, target: Path, target_faces: int) -> None:
    loaded = trimesh.load(source, force="mesh")
    components = loaded.split(only_watertight=False)
    if not components:
        raise ValueError(f"Malla vacía: {source}")
    mesh = max(components, key=lambda part: len(part.faces))
    mesh.fix_normals()
    trimesh.smoothing.filter_taubin(mesh, lamb=.45, nu=.5, iterations=4)
    if len(mesh.faces) > target_faces:
        parts = pymeshlab.MeshSet()
        parts.add_mesh(pymeshlab.Mesh(vertex_matrix=mesh.vertices, face_matrix=mesh.faces))
        parts.meshing_decimation_quadric_edge_collapse(
            targetfacenum=target_faces, preservenormal=True,
            preservetopology=True, autoclean=True,
        )
        cleaned = parts.current_mesh()
        mesh = trimesh.Trimesh(vertices=cleaned.vertex_matrix(), faces=cleaned.face_matrix())
        mesh.fix_normals()
    if not mesh.is_watertight or not np.isfinite(mesh.vertices).all():
        raise ValueError(f"La malla de Unity tiene bordes abiertos o coordenadas inválidas: {source}")
    vertices = (mesh.vertices - mesh.bounds[0]) / mesh.extents
    planes = np.abs(mesh.face_normals).argmax(axis=1)
    target.parent.mkdir(parents=True, exist_ok=True)
    with target.open("w", encoding="ascii", newline="\n") as output:
        output.write("# Operation Heist, original Hunyuan3D geometry\n")
        output.write(f"o {target.stem}\n")
        for x, y, z in mesh.vertices:
            output.write(f"v {x:.8f} {y:.8f} {z:.8f}\n")
        for x, y, z in mesh.vertex_normals:
            output.write(f"vn {x:.8f} {y:.8f} {z:.8f}\n")
        for face, plane in zip(mesh.faces, planes, strict=True):
            axes = [axis for axis in range(3) if axis != plane]
            for u, v in vertices[face][:, axes]:
                output.write(f"vt {u:.8f} {v:.8f}\n")
        for index, face in enumerate(mesh.faces):
            entries = [f"{vertex + 1}/{index * 3 + corner + 1}/{vertex + 1}" for corner, vertex in enumerate(face)]
            output.write("f " + " ".join(entries) + "\n")
    print(f"UNITY_OBJ_OK {target.name} {len(mesh.faces)} triangulos")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("target", type=Path)
    parser.add_argument("--triangles", default=10000, type=int)
    options = parser.parse_args()
    prepare(options.source, options.target, options.triangles)
