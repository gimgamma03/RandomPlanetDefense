# -*- coding: utf-8 -*-
import re
import uuid
from pathlib import Path

root = Path(r"c:/GitHub/RandomPlanetDefense/Assets/Resources/TowerData")
csv_paths = [
    Path(r"c:/GitHub/RandomPlanetDefense/Assets/Resources/TowerBalance.csv"),
    Path(r"c:/GitHub/RandomPlanetDefense/Balance/TowerBalance.csv"),
]

names = {
    "Cannon": "캐논타워",
    "Laser": "레이저타워",
    "Slow": "감속타워",
    "ChainLightning": "라이트닝타워",
    "Bomb": "폭탄타워",
    "MultiShot": "멀티샷타워",
    "ChargePierce": "차징타워",
    "OrbitSatellite": "위성타워",
}

csv_base = {
    "Cannon": dict(
        damage=5, rate=2, range=3, slowValue=0, spawnWeight=1, sell=0, doubleShot=0,
        upgradeDamage=1.5, upgradeRate=0, upgradeRange=0, upgradeSlow=0,
    ),
    "Laser": dict(
        damage=2, rate=1, range=2, slowValue=0, spawnWeight=1, sell=0, doubleShot=0,
        upgradeDamage=1, upgradeRate=0, upgradeRange=0, upgradeSlow=0,
    ),
    "MultiShot": dict(
        damage=2, rate=1.5, range=3, slowValue=0, spawnWeight=1, sell=0, doubleShot=0,
        upgradeDamage=1.2, upgradeRate=-0.05, upgradeRange=0, upgradeSlow=0,
    ),
    "Bomb": dict(
        damage=4, rate=1.6, range=3, slowValue=0, spawnWeight=1, sell=0, doubleShot=0,
        upgradeDamage=1.2, upgradeRate=0, upgradeRange=0.05, upgradeSlow=0,
    ),
    "Slow": dict(
        damage=0, rate=0, range=2.5, slowValue=0.2, spawnWeight=0.7, sell=0, doubleShot=0,
        upgradeDamage=0, upgradeRate=0, upgradeRange=0.08, upgradeSlow=0.02,
    ),
    "ChainLightning": dict(
        damage=3, rate=1.6, range=2.8, slowValue=0, spawnWeight=0.7, sell=0, doubleShot=0,
        upgradeDamage=1, upgradeRate=0, upgradeRange=0, upgradeSlow=0,
    ),
    "ChargePierce": dict(
        damage=6, rate=1.5, range=3.5, slowValue=0, spawnWeight=0.8, sell=0, doubleShot=0,
        upgradeDamage=1.5, upgradeRate=0, upgradeRange=0.05, upgradeSlow=0,
    ),
    "OrbitSatellite": dict(
        damage=2, rate=0, range=2.5, slowValue=0, spawnWeight=0.8, sell=0, doubleShot=0,
        upgradeDamage=0.5, upgradeRate=0, upgradeRange=0.1, upgradeSlow=0,
    ),
}

multi_by_g = {1: 3, 2: 3, 3: 4, 4: 5, 5: 5}
orbit_by_g = {1: 2, 2: 3, 3: 3, 4: 4, 5: 5}
bomb_by_g = {1: 5, 2: 5, 3: 6, 4: 7, 5: 8}


def r2(x):
    return round(x + 1e-9, 2)


def r3(x):
    return round(x + 1e-9, 3)


def scale(base, type_key, g):
    f = g - 1
    dmg = r2(base["damage"] * (1.5 ** f)) if base["damage"] > 0 else 0
    if base["rate"] <= 0:
        rate = 0
    elif type_key == "ChargePierce":
        rate = r2(max(0.35, base["rate"] * (0.88 ** f)))
    else:
        rate = r2(max(0.25, base["rate"] * (0.9 ** f)))
    rng = r2(base["range"] * (1.08 ** f))
    slow = r3(min(0.65, base["slowValue"] + 0.05 * f)) if base["slowValue"] > 0 else 0
    up_d = r2(base["upgradeDamage"] * (1.35 ** f)) if base["upgradeDamage"] else 0
    up_r = r3(base["upgradeRate"])
    up_rg = r3(base["upgradeRange"] * (1.1 ** f)) if base["upgradeRange"] else 0
    up_s = r3(base["upgradeSlow"] * (1.15 ** f)) if base["upgradeSlow"] else 0
    return dict(
        damage=dmg,
        rate=rate,
        range=rng,
        slowValue=slow,
        spawnWeight=base["spawnWeight"],
        sell=base["sell"],
        doubleShot=base["doubleShot"],
        upgradeDamage=up_d,
        upgradeRate=up_r,
        upgradeRange=up_rg,
        upgradeSlow=up_s,
    )


def yaml_unicode(s):
    return '"' + "".join(f"\\u{ord(ch):04X}" for ch in s) + '"'


templates = {}
for p in sorted(root.glob("G1_*.asset")):
    text = p.read_text(encoding="utf-8-sig")
    key = re.search(r"G1_(\w+)", p.stem).group(1)
    templates[key] = text

created = []
csv_rows = [
    "towerId,displayName,grade,damage,rate,range,slowValue,spawnWeight,sell,doubleShot,upgradeDamage,upgradeRate,upgradeRange,upgradeSlow"
]

# Keep type order stable for CSV readability
type_order = [
    "Cannon",
    "Laser",
    "MultiShot",
    "Bomb",
    "Slow",
    "ChainLightning",
    "ChargePierce",
    "OrbitSatellite",
]

for g in range(1, 6):
    for key in type_order:
        base = csv_base[key]
        s = scale(base, key, g) if g > 1 else dict(base)
        tid = f"G{g}_{key}"
        dname = names[key]
        csv_rows.append(
            f"{tid},{dname},{g},{s['damage']},{s['rate']},{s['range']},{s['slowValue']},"
            f"{s['spawnWeight']},{s['sell']},{s['doubleShot']},{s['upgradeDamage']},"
            f"{s['upgradeRate']},{s['upgradeRange']},{s['upgradeSlow']}"
        )

for key, tmpl in templates.items():
    base = csv_base[key]
    sprite = re.search(r"sprite: .*", tmpl).group(0)
    wtype = re.search(r"weaponType: .*", tmpl).group(0)
    ptype = re.search(r"projectileType: .*", tmpl).group(0)
    spread = re.search(r"multiShotSpreadAngle: .*", tmpl).group(0)
    line_len = re.search(r"groundBombLineLength: .*", tmpl).group(0)
    spawn_iv = re.search(r"groundBombSpawnInterval: .*", tmpl).group(0)
    laser_m = re.search(r"laserWidth: ([0-9.]+)", tmpl)
    laser0 = float(laser_m.group(1)) if laser_m else 0.0

    for g in range(2, 6):
        s = scale(base, key, g)
        tid = f"G{g}_{key}"
        dname = names[key]
        laser = r3(laser0 * (1.15 ** (g - 1))) if laser0 > 0 else 0
        multi = multi_by_g[g]
        orbit = orbit_by_g[g]
        bombs = bomb_by_g[g]
        dname_yaml = yaml_unicode(dname)

        content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 1fc33be49ddaf5d4e90c75ccc924b5ae, type: 3}}
  m_Name: {tid}
  m_EditorClassIdentifier: 
  towerId: {tid}
  displayName: {dname_yaml}
  weapon:
    damage: {s['damage']}
    rate: {s['rate']}
    range: {s['range']}
    sell: {s['sell']}
    doubleShot: {s['doubleShot']}
    slowValue: {s['slowValue']}
  weaponUpGradeValue:
    damage: {s['upgradeDamage']}
    rate: {s['upgradeRate']}
    range: {s['upgradeRange']}
    slowValue: {s['upgradeSlow']}
  grade: {g}
  {wtype}
  {ptype}
  {sprite}
  spriteColor: {{r: 1, g: 1, b: 1, a: 1}}
  spawnWeight: {s['spawnWeight']}
  multiShotCount: {multi}
  {spread}
  groundBombCount: {bombs}
  {line_len}
  {spawn_iv}
  laserWidth: {laser}
  orbitSatelliteCount: {orbit}
"""
        out = root / f"{tid}.asset"
        out.write_text(content, encoding="utf-8", newline="\n")
        guid = uuid.uuid4().hex
        meta = f"""fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
        (root / f"{tid}.asset.meta").write_text(meta, encoding="utf-8", newline="\n")
        created.append(tid)

csv_text = "\n".join(csv_rows) + "\n"
for cp in csv_paths:
    cp.write_text(csv_text, encoding="utf-8", newline="\n")

print(f"created {len(created)} assets")
print(f"csv rows {len(csv_rows) - 1}")
for tid in created:
    print(tid)
