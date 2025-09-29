# ForceGrip Unity Demo Project

[![Representative Image](https://han-dongheun.github.io/ForceGrip/Representative_Image.jpg)](https://han-dongheun.github.io/ForceGrip/)

---

This repository contains the Unity demo project for [**ForceGrip**](https://han-dongheun.github.io/ForceGrip/), a research project presented at **SIGGRAPH 2025**.
It introduces a reference-free curriculum learning approach for realistic grip force control in VR hand manipulation.

---

## 🧩 How to Run
- Unity Editor version: **2022.3.10f1** (recommended)
- Open `DemoScene.unity` in the editor.
  - This scene simplifies the **pick-and-place** and **can-squeeze** tasks described in the paper, so you can directly experience the demo.
- Connect your **Meta Quest** headset via **Meta Quest Link (USB cable)** or **Air Link (wireless)**.
- Press the **Play** button in Unity Editor.
  - Tested on **Meta Quest 2**, and it should work without additional setup.
  - Other Meta headsets may work, but have not been fully tested.

### ⚠️ Notes
- If it does not work correctly on your device, or if you want to connect a different hardware/method,
  you will need to adjust the **wrist transform** and **trigger value input** code sections to control the hand agent.
- Pre-built binaries are **not provided** — please use the Unity Editor to run the project.
- If you encounter issues, please open a **GitHub Issue** in this repository.
  > Immediate fixes may not be possible, but we will try to respond as best as we can.

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
