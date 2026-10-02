# MHR.Segment

Builds the 20-part body segmentation of the MHR mesh and measures which body parts each of the 321 parameters affects. C# port of the upstream [mhr_create_segmentation](https://github.com/facebookresearch/MHR/tree/main/tools/mhr_create_segmentation) tool, extended with a per-parameter effect analysis.

## How it works

1. **Segmentation** (`MhrSegmentation` in MHR.Net):
   - The 127 skeleton joints are grouped into 20 parts by name: fingers + wrist → `l_hand`/`r_hand`, foot bones → `l_foot`/`r_foot`, twist/helper joints merge into their main joint (`l_upleg_twist2_proc` → `l_upleg`), facial detail joints (eye, tongue, teeth…) → `zero_weight`.
   - Each vertex is assigned to the part of its dominant skinning influence (`get_lbsw()` from the TorchScript model). Soft per-part weights are also available.
2. **Parameter effects**: each parameter is set to the same 4 variants as MHR.Sweep (half and full range in both directions), then per part:
   - **Deformation share**: fraction of the total mesh edge-length change. It is insensitive to rigid motion, so for a joint rotation it points at the joint (e.g. `l_elbow_bend` → forearm + upper arm), not at the hand carried along.
   - **Motion share**: fraction of the total vertex displacement (what moves).
   - **Rigid**: the mesh moved without changing shape (e.g. `root_*`).

## Parts

| Part | Name | Part | Name |
|---|---|---|---|
| `root` | Pelvis | `c_spine` | Spine / torso |
| `l_upleg` / `r_upleg` | Thigh | `c_neck` | Neck |
| `l_lowleg` / `r_lowleg` | Shin | `c_head` | Head |
| `l_foot` / `r_foot` | Foot | `c_jaw` | Jaw |
| `l_clavicle` / `r_clavicle` | Clavicle | `l_uparm` / `r_uparm` | Upper arm |
| `l_lowarm` / `r_lowarm` | Forearm | `l_hand` / `r_hand` | Hand |
| `zero_weight` | Face detail joints (no vertices) | | |

`l_`/`r_` are the character's left/right (+X is the character's left).

## Output

Saved to `segment_output/` in the build directory:

- `part_segments.json` - Parts (key, display name, color, joints, vertex indices) and the per-vertex part index
- `param_segments.json` - Per-parameter deformation/motion share for every affected part
- `param_segments.csv` - Same in CSV: top part, deformed parts, moved parts

The same measurement is written by **MHR.Sweep** into `sweep_metadata.json` (`AffectedParts`) and passed to the vision model by **MHR.Identify**. The **MHR** app can color the body by part ("Show body part segments").

---
Generated with [Claude Code](https://claude.ai/claude-code) (Anthropic)
