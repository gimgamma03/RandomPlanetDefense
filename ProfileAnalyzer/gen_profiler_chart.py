import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
import matplotlib.font_manager as fm
import numpy as np
from pathlib import Path

font_path = r"C:\Windows\Fonts\malgun.ttf"
fm.fontManager.addfont(font_path)
plt.rcParams["font.family"] = fm.FontProperties(fname=font_path).get_name()
plt.rcParams["axes.unicode_minus"] = False

# Player Build Deep mean ms/frame (2026-07-29)
abs_b = [1.16, 0.035, 7.4]
abs_a = [0.44, 0.007, 7.0]
labels = ["타겟 탐색", "HUD TMP 갱신", "활성 CPU (추정)"]
pct_b = [100, 100, 100]
pct_a = [a / b * 100 for a, b in zip(abs_a, abs_b)]

x = np.arange(len(labels))
w = 0.36
fig, ax = plt.subplots(figsize=(7.4, 4.7), dpi=170)
fig.patch.set_facecolor("#ffffff")
ax.set_facecolor("#ffffff")

bars_b = ax.bar(x - w / 2, pct_b, w, label="최적화 전 Before", color="#9aa3ad")
bars_a = ax.bar(x + w / 2, pct_a, w, label="최적화 후 After", color="#2f9e7a")

ax.set_ylabel("Before = 100%", fontsize=10, color="#4a5560")
ax.set_xticks(x)
ax.set_xticklabels(labels, fontsize=11, color="#1a2332")
ax.set_ylim(0, 125)
ax.axhline(100, color="#d0d7de", lw=1, ls="--", zorder=0)
ax.spines["top"].set_visible(False)
ax.spines["right"].set_visible(False)
ax.spines["left"].set_color("#d0d7de")
ax.spines["bottom"].set_color("#d0d7de")
ax.tick_params(colors="#5c6b7a")
ax.yaxis.grid(True, color="#e8edf2", linewidth=0.8)
ax.set_axisbelow(True)

for i, (b, a) in enumerate(zip(bars_b, bars_a)):
    ax.text(
        b.get_x() + b.get_width() / 2,
        100 + 3,
        f"{abs_b[i]:.3g} ms",
        ha="center",
        va="bottom",
        fontsize=8,
        color="#64748b",
    )
    ax.text(
        a.get_x() + a.get_width() / 2,
        a.get_height() + 2.5,
        f"{abs_a[i]:.3g} ms\n({pct_a[i]:.0f}%)",
        ha="center",
        va="bottom",
        fontsize=8.5,
        color="#1a2332",
    )

ax.legend(frameon=False, loc="upper right", fontsize=9)
ax.set_title(
    "RPD · Player Build Deep Profile  (2026-07-29)",
    fontsize=12,
    pad=14,
    color="#1a2332",
    loc="left",
    fontweight="600",
)
fig.text(
    0.02,
    0.015,
    "Build Deep mean ms/frame · 타겟·HUD 중심 개선 · 일부 스파이크 잔존",
    fontsize=8.5,
    color="#6b7280",
)
fig.text(
    0.98,
    0.965,
    "Before / After",
    ha="right",
    va="top",
    fontsize=9,
    color="#2f9e7a",
    fontweight="600",
)
fig.tight_layout(rect=[0.02, 0.05, 0.98, 0.93])

for out in [
    Path(r"C:\GitHub\MyPortfolio\rpd-profiler-before-after.png"),
    Path(r"C:\GitHub\RandomPlanetDefense\ProfileAnalyzer\rpd-profiler-before-after.png"),
    Path(r"C:\GitHub\RandomPlanetDefense\Builds\rpd-profiler-before-after.png"),
]:
    out.parent.mkdir(parents=True, exist_ok=True)
    fig.savefig(out, dpi=170, facecolor="white", bbox_inches="tight", pad_inches=0.22)
    print("saved", out)
