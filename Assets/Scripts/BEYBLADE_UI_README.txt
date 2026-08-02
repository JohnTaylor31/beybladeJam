# Beyblade Health Bar UI Setup

This document explains how to set up and use the Beyblade rotation speed health bar UI system.

## Overview

The health bar UI displays each Beyblade's rotation speed (`spinSpeed`) as a visual health bar that:
- Shows spin speed as a percentage of maximum spin speed (default: 3000 RPM)
- Changes color based on spin speed:
  - **Green** = High spin speed (above 50%)
  - **Yellow** = Medium spin speed (around 50%)
  - **Red** = Low spin speed (below 50%)
- Displays the actual spin speed number in the center of the bar

## Automatic Setup (Recommended)

The UI is created automatically when the scene starts:

1. **Add BeybladeUIManager to the Scene**
   - Create an empty GameObject in your scene
   - Add the `BeybladeUIManager` script component to it
   - Optionally customize the colors and bar size in the Inspector

2. **Configure Settings (Optional)**
   - `Player 1 Color`: Color for the player 1 health bar (default: Blue)
   - `Player 2 Color`: Color for the player 2 health bar (default: Red)
   - `Max Spin Speed`: Maximum spin speed for scaling (default: 3000)
   - `Bar Width`: Width of the health bar in pixels (default: 200)
   - `Bar Height`: Height of the health bar in pixels (default: 30)
   - `Bar Spacing`: Space from screen edge (default: 10)

3. **Play**
   - When you press Play, the UI will automatically:
     - Create a Canvas if one doesn't exist
     - Create two health bars (one for each Beyblade)
     - Position them at the top corners of the screen
     - Start tracking rotation speed

## Manual Setup (Advanced)

If you want to manually create health bars:

1. Create a Canvas (if one doesn't exist)
2. Create a Panel or Image for each health bar background
3. Add an Image component as a child (this will be the fill bar)
4. Add a Text component for displaying the spin speed
5. Attach `BeybladeHealthBar` script to the container
6. In the Inspector:
   - Assign the fill Image to `Health Bar Fill`
   - Assign the Text to `Spin Speed Text`
   - Assign the Beyblade's GameObject to `Beyblade Controller`
   - Set `Max Spin Speed` (default: 3000)
   - Customize the colors

## Health Bar Colors

The bar uses a gradient system:

- **High Spin (100%)**: Full color (green/blue/red depending on player)
- **Medium Spin (50%)**: Blend to yellow
- **Low Spin (0%)**: Red

You can customize these colors in the `BeybladeHealthBar` component:
- `High Spin Color`: Color when at max spin
- `Medium Spin Color`: Color at 50% spin
- `Low Spin Color`: Color when at 0% spin

## Troubleshooting

**Issue: Health bar not showing**
- Make sure `BeybladeUIManager` is in the scene
- Check that Beyblades have `BeybladeController` component attached
- Verify Canvas is created (check Hierarchy for "BeybladeUICanvas")

**Issue: Incorrect spin values**
- The `Max Spin Speed` value should match your Beyblade's initial spin
- Adjust `Max Spin Speed` in the UI Manager to scale the bar correctly

**Issue: Health bar not updating**
- Ensure `spinSpeed` field on BeybladeController is public (it is by default)
- Check that the Beyblade GameObject is assigned correctly

## Technical Details

- The UI uses reflection to read the `spinSpeed` value from `BeybladeController`
- This allows the UI to work without direct class references
- The health bar updates every frame in the `Update()` method
- Colors are calculated using `Color.Lerp()` for smooth gradients
