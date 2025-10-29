# ForceGrip Unity Demo Project

[![Representative Image](https://han-dongheun.github.io/ForceGrip/Representative_Image.jpg)](https://han-dongheun.github.io/ForceGrip/)

---

This repository contains the Unity demo project for [**ForceGrip**](https://han-dongheun.github.io/ForceGrip/), a research project presented at **SIGGRAPH 2025**.
It introduces a reference-free curriculum learning approach for realistic grip force control in VR hand manipulation.

| Pick and Place | Can Squeeze |
|-------------------|-------------------|
| ![PickandPlaceTask](docs/PickandPlaceTask.gif) | ![CanSqueezeTask](docs/CanSqueezeTask.gif) |

---

## 🧩 How to Run
- Clone this repository (Git LFS in use, ZIP download not supported)
- Unity Editor version: **2022.3.10f1** (recommended)
- Only the folder **`ForceGrip_UnityProject_ForDemo`** is required.  
- Open `DemoScene.unity` in the editor.
  - This scene simplifies the **pick-and-place** and **can-squeeze** tasks described in the paper, so you can directly experience the demo.
- Connect your **Meta Quest** headset via **Meta Quest Link (USB cable)** or **Air Link (wireless)**.
- Press the **Play** button in Unity Editor.
  - Tested on **Meta Quest 2**, and it should work without additional setup.
  - Other Meta headsets may work, but have not been fully tested.
- ⚠️ To properly fetch required Unity packages, **Git must be installed** on your system.

### ⚠️ Notes
- If it does not work correctly on your device, or if you want to connect a different hardware/method,
  you will need to adjust the **wrist transform** and **trigger value input** code sections to control the hand agent.
- Pre-built binaries are **not provided** — please use the Unity Editor to run the project.
- **Windows only**. Other platforms require manual modifications.
- All code may not be perfectly organized for other settings.
  If you encounter issues, please open a **GitHub Issue** in this repository.
  > Immediate fixes may not be possible, but we will try to respond as best as we can.
- If you need the **training code**, please contact me by email.
  > The training code is not organized for release, but may be shared depending on the situation.

### Tips
- The current training version is configured so that the **trigger value (0–1)** corresponds to a **grip force of 0–10 kg**.
- A maximum force of 10 kg can be quite strong for grasping lightweight objects.
- Because the **Oculus hand** uses rather **blunt, rounded capsule colliders**, this can sometimes cause **slipping** during grasping.
- To adjust this, set the parameter **`totalForceAdjust_Kg`** in the `AgentsInferenceManager` to a value **below 10 kg**.
This changes the maximum output force range to **0 – `totalForceAdjust_Kg`**.
- If you want to focus on **stable grasping** rather than maximum strength, try setting this value to **maybe 1kg or 3 kg or 5 kg** for testing.

---

## 📚 Citation
If you use this project in academic work, please cite:

```bibtex
@inproceedings{Han2025ForceGrip,
  author = {DongHeun Han and Byungmin Kim and RoUn Lee and KyeongMin Kim and Hyoseok Hwang and HyeongYeop Kang},
  title = {ForceGrip: Reference-Free Curriculum Learning for Realistic Grip Force Control in VR Hand Manipulation},
  booktitle = {SIGGRAPH Conference Papers '25},
  year = {2025},
  pages = {1--11},
  doi = {10.1145/3721238.3730738},
  url = {https://doi.org/10.1145/3721238.3730738},
  note = {https://han-dongheun.github.io/ForceGrip}
}
