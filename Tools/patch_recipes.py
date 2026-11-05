"""
patch_recipes.py — Operation Heist
Mejora las recetas de los 9 modelos prioritarios en ModelRecipes.json.
Ejecucion: python Tools/patch_recipes.py  (desde la raiz del proyecto)
Sin dependencias externas; solo stdlib.
"""
import json, copy, sys

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def vec(x, y, z): return {"x": x, "y": y, "z": z}
def part(name, pos, size, mat, group="", pivot=None, shape=0, rot=None):
    p = {
        "name": name,
        "position": pos,
        "size": size,
        "material": mat,
        "group": group,
        "pivot": pivot if pivot else vec(0, 0, 0),
        "shape": shape,
        "rotation": rot if rot else vec(0, 0, 0),
    }
    return p

# Material palette indices (same as ProductionModelsBuilder):
#  0=Steel(azul gris), 1=Dark(negro), 2=Brass(dorado), 3=Ivory(marfil),
#  4=Wood(madera), 5=Teal(verde azul), 6=Red(rojo), 7=Cloth(verde oscuro),
#  8=Skin(piel), 9=Screen(azul pantalla)
STEEL  = 0
DARK   = 1
BRASS  = 2
IVORY  = 3
WOOD   = 4
TEAL   = 5
RED    = 6
CLOTH  = 7
SKIN   = 8
SCREEN = 9

# shape constants
CUBE     = 0
CYLINDER = 1
INGOT    = 2
SPHERE   = 3

# ---------------------------------------------------------------------------
# Character factory
# ---------------------------------------------------------------------------

def make_character(name, cat, torso_mat, leg_mat, boot_mat, skin_mat, head_mat,
                   shirt_accent_mat, belt_mat, hat_mat,
                   height=1.84):
    """
    Builds a full humanoid recipe with:
    - Rounded head (sphere shape=3)
    - Neck cylinder
    - Torso with collar/belt detail
    - Shoulders wider than before
    - Forearms and hands (cylinder+cube)
    - Hips with subtle flare
    - Shins that taper into distinct boots
    All joints preserved: Spine, Head, LeftShoulder, LeftElbow,
    RightShoulder, RightElbow, LeftHip, LeftKnee, RightHip, RightKnee
    """
    parts = []

    # ---- ROOT (legs, boots, hips belt) ----
    # Left boot
    parts.append(part("LeftBoot",   vec(-0.14,0.1,0),    vec(0.17,0.20,0.22), boot_mat))
    # Right boot
    parts.append(part("RightBoot",  vec( 0.14,0.1,0),    vec(0.17,0.20,0.22), boot_mat))
    # Belt/waist band
    parts.append(part("Belt",       vec(0,0.72,0),        vec(0.46,0.07,0.24), belt_mat))

    # ---- SPINE joint (torso + shoulders + neck) ----
    piv_spine = vec(0, 0.76, 0)
    # Torso body
    parts.append(part("Torso",      vec(0,1.13,0),        vec(0.42,0.74,0.22), torso_mat,  "Spine", piv_spine))
    # Collar
    parts.append(part("Collar",     vec(0,1.51,0),        vec(0.34,0.07,0.18), shirt_accent_mat, "Spine", piv_spine))
    # Neck
    parts.append(part("Neck",       vec(0,1.60,0),        vec(0.12,0.12,0.12), skin_mat,   "Spine", piv_spine, CYLINDER))

    # ---- HEAD joint (child of Spine) ----
    piv_head = vec(0, 1.63, 0)
    # Skull sphere
    parts.append(part("Skull",      vec(0,1.78,0),        vec(0.27,0.27,0.27), skin_mat,   "Head", piv_head, SPHERE))
    # Hat/hair cap on top
    parts.append(part("HatBrim",    vec(0,1.92,0),        vec(0.31,0.04,0.31), hat_mat,    "Head", piv_head))
    parts.append(part("HatTop",     vec(0,1.97,0),        vec(0.22,0.10,0.22), hat_mat,    "Head", piv_head))

    # ---- LEFT SHOULDER ----
    piv_ls = vec(-0.25, 1.42, 0)
    parts.append(part("UpperArm",   vec(-0.36,1.25,0),    vec(0.13,0.34,0.13), torso_mat,  "LeftShoulder",  piv_ls, CYLINDER))
    # LEFT ELBOW (child of LeftShoulder)
    piv_le = vec(-0.36, 1.08, 0)
    parts.append(part("Forearm",    vec(-0.36,0.93,0),    vec(0.11,0.30,0.11), skin_mat,   "LeftElbow",  piv_le, CYLINDER))
    parts.append(part("Hand",       vec(-0.36,0.76,0),    vec(0.11,0.12,0.10), skin_mat,   "LeftElbow",  piv_le))

    # ---- RIGHT SHOULDER ----
    piv_rs = vec(0.25, 1.42, 0)
    parts.append(part("UpperArm",   vec(0.36,1.25,0),     vec(0.13,0.34,0.13), torso_mat,  "RightShoulder", piv_rs, CYLINDER))
    # RIGHT ELBOW (child of RightShoulder)
    piv_re = vec(0.36, 1.08, 0)
    parts.append(part("Forearm",    vec(0.36,0.93,0),     vec(0.11,0.30,0.11), skin_mat,   "RightElbow", piv_re, CYLINDER))
    parts.append(part("Hand",       vec(0.36,0.76,0),     vec(0.11,0.12,0.10), skin_mat,   "RightElbow", piv_re))

    # ---- LEFT HIP ----
    piv_lh = vec(-0.13, 0.72, 0)
    parts.append(part("Thigh",      vec(-0.13,0.50,0),    vec(0.16,0.42,0.16), leg_mat,    "LeftHip",  piv_lh, CYLINDER))
    # LEFT KNEE
    piv_lk = vec(-0.13, 0.28, 0)
    parts.append(part("Shin",       vec(-0.13,0.18,0),    vec(0.14,0.36,0.14), leg_mat,    "LeftKnee", piv_lk, CYLINDER))

    # ---- RIGHT HIP ----
    piv_rh = vec(0.13, 0.72, 0)
    parts.append(part("Thigh",      vec(0.13,0.50,0),     vec(0.16,0.42,0.16), leg_mat,    "RightHip",  piv_rh, CYLINDER))
    # RIGHT KNEE
    piv_rk = vec(0.13, 0.28, 0)
    parts.append(part("Shin",       vec(0.13,0.18,0),     vec(0.14,0.36,0.14), leg_mat,    "RightKnee", piv_rk, CYLINDER))

    return {"name": name, "category": cat, "parts": parts}


def make_agent():
    return make_character(
        "Agent", "Characters",
        torso_mat=DARK,     # black tactical vest
        leg_mat=DARK,
        boot_mat=STEEL,
        skin_mat=SKIN,
        head_mat=DARK,
        shirt_accent_mat=STEEL,
        belt_mat=BRASS,
        hat_mat=DARK,
    )

def make_guard():
    # Override palette slot 6 (Red) to a warm navy for guard uniform
    rec = make_character(
        "Guard", "Characters",
        torso_mat=5,        # Teal → re-used as navy (override below)
        leg_mat=5,
        boot_mat=STEEL,
        skin_mat=SKIN,
        head_mat=5,
        shirt_accent_mat=IVORY,
        belt_mat=BRASS,
        hat_mat=5,
    )
    # materialOverrides: slot 5 (Teal) → navy blue for guard
    # format: [r,g,b,a, r,g,b,a, ...] — only slot 5 overridden (skip 0-4 with a=0)
    rec["materialOverrides"] = [
        0,0,0,0,  # 0 Steel — keep
        0,0,0,0,  # 1 Dark  — keep
        0,0,0,0,  # 2 Brass — keep
        0,0,0,0,  # 3 Ivory — keep
        0,0,0,0,  # 4 Wood  — keep
        0.12, 0.20, 0.42, 1.0,  # 5 → navy blue
    ]
    return rec

def make_bankclerk():
    rec = make_character(
        "BankClerk", "Characters",
        torso_mat=IVORY,    # white shirt
        leg_mat=DARK,       # dark trousers
        boot_mat=DARK,
        skin_mat=SKIN,
        head_mat=SKIN,
        shirt_accent_mat=BRASS,  # brass tie
        belt_mat=DARK,
        hat_mat=SKIN,
    )
    # Add a tie as a spine sub-part
    piv_spine = vec(0, 0.76, 0)
    rec["parts"].append(part("Tie", vec(0,1.27,0), vec(0.05,0.34,0.02), BRASS, "Spine", piv_spine))
    return rec

# ---------------------------------------------------------------------------
# Individual improved recipes
# ---------------------------------------------------------------------------

def make_dossier():
    """Thicker, highly visible red folder with spine label strip."""
    parts = [
        part("Cover",  vec(0, 0.025, 0),    vec(0.32, 0.050, 0.24), RED),
        part("Pages",  vec(0, 0.058, 0),    vec(0.29, 0.018, 0.22), IVORY),
        part("Spine",  vec(-0.145, 0.037, 0), vec(0.025, 0.060, 0.24), BRASS),
        part("Label",  vec(0.06, 0.063, 0), vec(0.16, 0.010, 0.14), IVORY),
        part("Clasp",  vec(0.13, 0.063, 0), vec(0.035, 0.012, 0.035), BRASS),
    ]
    return {"name": "Dossier", "category": "Loot", "parts": parts}

def make_lootbag():
    """
    Duffel bag: rounded body (sphere), flat bottom, carry strap, end caps and zip pull.
    Lying on side (long axis = X).
    """
    parts = [
        # Main body — sphere squashed to bag proportions
        part("Body",      vec(0,0.26,0),      vec(0.62,0.46,0.32), CLOTH,  shape=SPHERE),
        # Flat base so it sits on ground
        part("Base",      vec(0,0.05,0),      vec(0.56,0.10,0.30), CLOTH),
        # End caps (cylinders on X axis)
        part("CapLeft",   vec(-0.29,0.26,0),  vec(0.06,0.42,0.28), DARK,   shape=CYLINDER),
        part("CapRight",  vec( 0.29,0.26,0),  vec(0.06,0.42,0.28), DARK,   shape=CYLINDER),
        # Top zip strip
        part("Zip",       vec(0,0.48,0),      vec(0.50,0.03,0.06), DARK),
        # Zip pull
        part("ZipPull",   vec(0.08,0.51,0),   vec(0.025,0.04,0.025), BRASS),
        # Carry handle
        part("Handle",    vec(0,0.52,0.06),   vec(0.22,0.04,0.03), DARK),
        # Strap buckle
        part("Buckle",    vec(-0.05,0.52,0.10), vec(0.055,0.04,0.015), BRASS),
    ]
    return {"name": "LootBag", "category": "Loot", "parts": parts}

def make_warehouseRack():
    """
    Two-bay pallet rack: two uprights, three levels of cross-beams,
    diagonal braces on the side face, and two load-deck planks per bay.
    Much more readable as industrial shelving vs the old plain box.
    """
    parts = []
    # Uprights (4 posts)
    for x in [-0.95, -0.02, 0.02, 0.95]:
        parts.append(part("Upright", vec(x,1.25,0), vec(0.08,2.50,0.08), STEEL))
    # Horizontal beams — 3 levels, front and back per level
    for y in [0.55, 1.25, 1.95]:
        for z in [-0.37, 0.37]:
            parts.append(part("Beam", vec(0,y,z), vec(2.04,0.07,0.07), DARK))
    # Diagonal braces on back face (X)
    parts.append(part("BraceL", vec(-0.47,1.25,0.38), vec(0.07,2.10,0.07), DARK, rot=vec(0,0, 45)))
    parts.append(part("BraceR", vec( 0.47,1.25,0.38), vec(0.07,2.10,0.07), DARK, rot=vec(0,0,-45)))
    # Shelf decks — two planks per level
    for y in [0.59, 1.29, 1.99]:
        for offset_x in [-0.48, 0.48]:
            parts.append(part("Deck", vec(offset_x,y,0), vec(0.90,0.04,0.74), WOOD))
    return {"name": "WarehouseRack", "category": "Depot", "parts": parts}

def make_tellerCounter():
    """
    Bank teller station: bullet-proof glass panel, solid lower counter,
    transaction slot, side panels and name placard rail.
    """
    parts = [
        # Lower solid counter
        part("CounterBase",   vec(0,0.45,0),        vec(2.60,0.90,0.50), IVORY),
        # Counter top edge trim
        part("CounterTop",    vec(0,0.91,0),         vec(2.62,0.04,0.52), STEEL),
        # Glass panel (screen material = teal translucent stand-in)
        part("GlassPanel",    vec(0,1.57,0.12),      vec(2.56,1.32,0.04), SCREEN),
        # Glass frame top bar
        part("GlassFrameTop", vec(0,2.24,0.12),      vec(2.60,0.07,0.06), STEEL),
        # Transaction slot (dark slit at base of glass)
        part("TxSlot",        vec(0,0.96,0.12),      vec(0.50,0.06,0.04), DARK),
        # Right side panel
        part("SideR",         vec(1.28,1.12,0),      vec(0.04,1.46,0.50), STEEL),
        # Left side panel
        part("SideL",         vec(-1.28,1.12,0),     vec(0.04,1.46,0.50), STEEL),
        # Name placard rail
        part("Placard",       vec(0,1.70,-0.22),     vec(0.90,0.10,0.04), BRASS),
    ]
    return {"name": "TellerCounter", "category": "Bank", "parts": parts}

def make_securityDoor():
    """
    Heavy-duty security door: frame, armoured slab door with rivets,
    warning stripe, keycard reader stub and push bar.
    """
    parts = [
        # Frame left/right jambs
        part("JambaL",  vec(-0.60,1.15,0),  vec(0.14,2.30,0.25), STEEL),
        part("JambaR",  vec( 0.60,1.15,0),  vec(0.14,2.30,0.25), STEEL),
        # Frame lintel
        part("Dintel",  vec(0,2.30,0),       vec(1.34,0.14,0.25), STEEL),
        # Door slab
        part("Hoja",    vec(0,1.12,0),       vec(1.05,2.24,0.10), DARK,    "Door", vec(-0.525,0,0)),
        # Armour face plate
        part("Placa",   vec(0,1.12,-0.06),   vec(0.96,2.10,0.02), STEEL,   "Door", vec(-0.525,0,0)),
        # Warning stripe (yellow/brass) across middle
        part("Stripe",  vec(0,1.12,-0.08),   vec(0.96,0.12,0.015),BRASS,   "Door", vec(-0.525,0,0)),
        # Handle / push bar
        part("Barra",   vec(0.30,1.12,-0.10),vec(0.38,0.05,0.04), BRASS,   "Door", vec(-0.525,0,0)),
        # Card reader stub on frame
        part("Lector",  vec(0.68,1.30,-0.14),vec(0.06,0.14,0.05), DARK),
        # Rivet row left edge of door
        part("RivetTL", vec(-0.39,1.80,-0.08),vec(0.025,0.025,0.015),BRASS,"Door", vec(-0.525,0,0)),
        part("RivetBL", vec(-0.39,0.55,-0.08),vec(0.025,0.025,0.015),BRASS,"Door", vec(-0.525,0,0)),
        # Rivet row right edge
        part("RivetTR", vec( 0.39,1.80,-0.08),vec(0.025,0.025,0.015),BRASS,"Door", vec(-0.525,0,0)),
        part("RivetBR", vec( 0.39,0.55,-0.08),vec(0.025,0.025,0.015),BRASS,"Door", vec(-0.525,0,0)),
    ]
    return {"name": "SecurityDoor", "category": "Architecture", "parts": parts}

def make_vaultDoor():
    """Keep existing structure but add a circular combination wheel (cylinder) 
    and strengthen the bolt/rivet visual."""
    # Re-use the existing well-crafted vault door recipe; just add wheel and improve bolts.
    # We'll fully define it here so it's clean.
    parts = [
        # Frame
        part("JambaL",     vec(-1.20,1.35,0),     vec(0.14,2.70,0.25), STEEL),
        part("JambaR",     vec( 1.20,1.35,0),     vec(0.14,2.70,0.25), STEEL),
        part("Dintel",     vec(0,2.70,0),           vec(2.54,0.14,0.25), STEEL),
        # Door slab (Door group, pivot at left edge)
        part("Hoja",       vec(0,1.32,0),           vec(2.15,2.55,0.35), STEEL,   "Door", vec(-1.075,0,0)),
        # Dark face plate
        part("Placa",      vec(0,1.32,-0.19),       vec(1.90,2.28,0.06), DARK,    "Door", vec(-1.075,0,0)),
        # 6 bolts around the perimeter
        part("Bolt0",      vec( 0.75, 1.32,-0.26),  vec(0.14,0.14,0.16), BRASS,   "Door", vec(-1.075,0,0)),
        part("Bolt1",      vec(-0.75, 1.32,-0.26),  vec(0.14,0.14,0.16), BRASS,   "Door", vec(-1.075,0,0)),
        part("Bolt2",      vec( 0.38, 2.10,-0.26),  vec(0.14,0.14,0.16), BRASS,   "Door", vec(-1.075,0,0)),
        part("Bolt3",      vec(-0.38, 2.10,-0.26),  vec(0.14,0.14,0.16), BRASS,   "Door", vec(-1.075,0,0)),
        part("Bolt4",      vec( 0.38, 0.54,-0.26),  vec(0.14,0.14,0.16), BRASS,   "Door", vec(-1.075,0,0)),
        part("Bolt5",      vec(-0.38, 0.54,-0.26),  vec(0.14,0.14,0.16), BRASS,   "Door", vec(-1.075,0,0)),
        # Central hub for combination wheel
        part("WheelHub",   vec(0, 1.32,-0.38),      vec(0.12,0.12,0.28), BRASS,   "Door", vec(-1.075,0,0), CYLINDER),
        # Wheel horizontal bar
        part("WheelH",     vec(0, 1.32,-0.53),      vec(0.70,0.065,0.07),BRASS,   "Door", vec(-1.075,0,0)),
        # Wheel vertical bar
        part("WheelV",     vec(0, 1.32,-0.53),      vec(0.065,0.70,0.07),BRASS,   "Door", vec(-1.075,0,0)),
        # Wheel outer ring (thin flat torus approximated as 8 short cylinders)
        # We use 4 arc segments (cylinders) for the ring
        part("RingN",      vec(0, 1.68,-0.53),      vec(0.08,0.08,0.07), BRASS,   "Door", vec(-1.075,0,0), CYLINDER),
        part("RingS",      vec(0, 0.96,-0.53),      vec(0.08,0.08,0.07), BRASS,   "Door", vec(-1.075,0,0), CYLINDER),
        part("RingE",      vec(0.36,1.32,-0.53),    vec(0.08,0.08,0.07), BRASS,   "Door", vec(-1.075,0,0), CYLINDER),
        part("RingW",      vec(-0.36,1.32,-0.53),   vec(0.08,0.08,0.07), BRASS,   "Door", vec(-1.075,0,0), CYLINDER),
    ]
    return {"name": "VaultDoor", "category": "Bank", "parts": parts}

# ---------------------------------------------------------------------------
# Apply patches
# ---------------------------------------------------------------------------

PATCHES = {
    "Agent":         make_agent,
    "Guard":         make_guard,
    "BankClerk":     make_bankclerk,
    "Dossier":       make_dossier,
    "LootBag":       make_lootbag,
    "WarehouseRack": make_warehouseRack,
    "TellerCounter": make_tellerCounter,
    "SecurityDoor":  make_securityDoor,
    "VaultDoor":     make_vaultDoor,
}

data = json.load(open("Tools/ModelRecipes.json", "r", encoding="utf-8"))

replaced = []
for i, m in enumerate(data["models"]):
    if m["name"] in PATCHES:
        data["models"][i] = PATCHES[m["name"]]()
        replaced.append(m["name"])

print("Patched models:", replaced)

# Validate: every part must have required fields, rotation is optional (legacy parts don't have it)
for m in data["models"]:
    for p in m["parts"]:
        for field in ("name", "position", "size", "material", "group", "pivot", "shape"):
            assert field in p, f"Missing '{field}' in {m['name']}.{p.get('name','?')}"
        for axis in ("x","y","z"):
            v = p["size"][axis]
            assert v > 0 and v == v, f"Invalid size.{axis}={v} in {m['name']}.{p['name']}"
        v = p["position"]["y"]
        assert v == v, f"NaN position.y in {m['name']}.{p['name']}"
        # Ensure ground clearance for non-character non-root parts
        # (characters can have parts below root pivot)

print("Validation OK")

with open("Tools/ModelRecipes.json", "w", encoding="utf-8") as f:
    json.dump(data, f, indent=2, ensure_ascii=False)

print("Done — Tools/ModelRecipes.json updated.")
