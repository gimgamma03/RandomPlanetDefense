# -*- coding: utf-8 -*-
"""Tag existing enemies as T1 / RunnerElite->Runner T2, generate missing T2/T3."""
import re
import uuid
from pathlib import Path

root = Path(r"c:/GitHub/RandomPlanetDefense/Assets/Resources/EnemyData")
script_guid = "2b3505e5ce6b5354bb7cad42f50dcbc2"


def r2(x):
    return round(float(x) + 1e-9, 2)


def r1(x):
    return int(round(float(x) + 1e-9))


def parse_asset(text):
    def grab(pat, default=None, cast=str):
        m = re.search(pat, text)
        if not m:
            return default
        return cast(m.group(1))

    return {
        "enemyType": grab(r"enemyType: (\d+)", 0, int),
        "enemyRole": grab(r"enemyRole: (\d+)", 0, int),
        "gold": grab(r"gold: (\d+)", 1, int),
        "scorePoint": grab(r"scorePoint: (\d+)", 10, int),
        "maxHp": grab(r"maxHp: ([0-9.]+)", 10, float),
        "moveSpeed": grab(r"moveSpeed: ([0-9.]+)", 2, float),
        "rotateSpeed": grab(r"rotateSpeed: ([0-9.]+)", 0, float),
        "visualScale": grab(r"visualScale: ([0-9.]+)", 1, float),
        "shieldHp": grab(r"shieldHp: ([0-9.]+)", 0, float),
        "splitCount": grab(r"splitCount: (\d+)", 2, int),
        "splitChildType": grab(r"splitChildType: (\d+)", 0, int),
        "sprite": grab(r"(sprite: .*)", "sprite: {fileID: 0}"),
        "spriteColor": grab(
            r"(spriteColor: \{[^}]+\})",
            "spriteColor: {r: 1, g: 1, b: 1, a: 1}",
        ),
    }


def scale_stats(base, tier, role):
    f = tier - 1
    hp = r2(base["maxHp"] * (1.6 ** f))
    if role == 1:  # Tank
        speed = r2(base["moveSpeed"] * (1.03 ** f))
    elif role == 0:  # Runner
        speed = r2(base["moveSpeed"] * (1.12 ** f))
    else:
        speed = r2(base["moveSpeed"] * (1.08 ** f))

    gold = max(1, r1(base["gold"] * (1.5 ** f)))
    score = max(1, r1(base["scorePoint"] * (1.5 ** f)))
    shield = r2(base["shieldHp"] * (1.5 ** f)) if base["shieldHp"] > 0 else 0
    visual = r2(base["visualScale"] * (1.06 ** f))
    split = base["splitCount"]
    if role == 4 and tier >= 3:  # Splitter T3
        split = base["splitCount"] + 1

    # slight tint boost per tier
    color = base["spriteColor"]
    return dict(
        maxHp=hp,
        moveSpeed=speed,
        gold=gold,
        scorePoint=score,
        shieldHp=shield,
        visualScale=visual,
        splitCount=split,
        rotateSpeed=r2(base["rotateSpeed"] * (1.05 ** f)) if base["rotateSpeed"] > 0 else 0,
        spriteColor=color,
    )


def write_asset(path, name, enemy_id, enemy_type, display, role, tier, base, stats):
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
  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier: 
  enemyId: {enemy_id}
  enemyType: {enemy_type}
  displayName: {display}
  enemyRole: {role}
  enemyTier: {tier}
  gold: {stats['gold']}
  scorePoint: {stats['scorePoint']}
  maxHp: {stats['maxHp']}
  moveSpeed: {stats['moveSpeed']}
  rotateSpeed: {stats['rotateSpeed']}
  visualScale: {stats['visualScale']}
  shieldHp: {stats['shieldHp']}
  splitCount: {stats['splitCount']}
  splitChildType: {base['splitChildType']}
  {base['sprite']}
  {stats['spriteColor']}
"""
    path.write_text(content, encoding="utf-8", newline="\n")
    meta_path = Path(str(path) + ".meta")
    if not meta_path.exists():
        meta_path.write_text(
            f"""fileFormatVersion: 2
guid: {uuid.uuid4().hex}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
            encoding="utf-8",
            newline="\n",
        )


# --- patch existing T1 / convert elite ---
patches = {
    "E01_Swarm.asset": dict(tier=1, enemy_id="Swarm_T1", display="Swarm"),
    "E02_Runner.asset": dict(tier=1, enemy_id="Runner_T1", display="Runner"),
    "E03_Tank.asset": dict(tier=1, enemy_id="Tank_T1", display="Tank"),
    "E04_Shielded.asset": dict(tier=1, enemy_id="Shielded_T1", display="Shielded"),
    "E05_Splitter.asset": dict(tier=1, enemy_id="Splitter_T1", display="Splitter"),
}

for fname, info in patches.items():
    p = root / fname
    text = p.read_text(encoding="utf-8-sig")
    if re.search(r"enemyTier:", text):
        text = re.sub(r"enemyTier: \d+", f"enemyTier: {info['tier']}", text)
    else:
        text = re.sub(
            r"(enemyRole: \d+\n)",
            rf"\1  enemyTier: {info['tier']}\n",
            text,
        )
    text = re.sub(r"enemyId: .*", f"enemyId: {info['enemy_id']}", text)
    text = re.sub(r"displayName: .*", f"displayName: {info['display']}", text)
    # keep file name; m_Name unchanged
    p.write_text(text, encoding="utf-8", newline="\n")
    print("patched", fname)

# RunnerElite → Runner T2
elite = root / "E06_RunnerElite.asset"
elite_text = elite.read_text(encoding="utf-8-sig")
elite_base = parse_asset(elite_text)
# rewrite as Runner T2
write_asset(
    elite,
    "E06_RunnerElite",
    "Runner_T2",
    enemy_type=1,  # Runner
    display="Runner",
    role=0,
    tier=2,
    base=elite_base,
    stats={
        "gold": elite_base["gold"],
        "scorePoint": elite_base["scorePoint"],
        "maxHp": elite_base["maxHp"],
        "moveSpeed": elite_base["moveSpeed"],
        "rotateSpeed": elite_base["rotateSpeed"],
        "visualScale": elite_base["visualScale"],
        "shieldHp": 0,
        "splitCount": elite_base["splitCount"],
        "spriteColor": elite_base["spriteColor"],
    },
)
print("converted E06_RunnerElite -> Runner T2")

# bases for generation
family = {
    "Swarm": ("E01_Swarm.asset", 0, 2, "E01"),
    "Runner": ("E02_Runner.asset", 1, 0, "E02"),
    "Tank": ("E03_Tank.asset", 2, 1, "E03"),
    "Shielded": ("E04_Shielded.asset", 3, 3, "E04"),
    "Splitter": ("E05_Splitter.asset", 4, 4, "E05"),
}

created = []
for name, (t1_file, etype, role, prefix) in family.items():
    t1 = parse_asset((root / t1_file).read_text(encoding="utf-8-sig"))
    # Runner T2 already from elite — only create T3 for Runner; T2/T3 for others
    tiers = [3] if name == "Runner" else [2, 3]
    for tier in tiers:
        fname = f"{prefix}_{name}_T{tier}.asset"
        # Runner T2 file name separate if we want - already E06. For T3:
        if name == "Runner" and tier == 3:
            fname = "E02_Runner_T3.asset"
        stats = scale_stats(t1, tier, role)
        write_asset(
            root / fname,
            Path(fname).stem,
            f"{name}_T{tier}",
            etype,
            name,
            role,
            tier,
            t1,
            stats,
        )
        created.append(fname)

print("created", created)
