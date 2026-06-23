Implementation of Quake III “vanilla” and Challenge ProMode Arena (CPMA) strafe jumping mechanics in the Unity engine.

This is an enhanced fork of [IsaiahKelly](https://github.com/IsaiahKelly) fork of the original scripts created by [WiggleWizard](https://github.com/WiggleWizard).
Most of the credit goes to them. My contributions were:
+ a small physics fix
+ crouching
+ walking
+ landing bounce
+ smooth step
+ auto bunny hopping
+ launch function (f.ex. for jump pads)
+ a configurable controller, including features, variables, and bindings
+ preconfigured prefabs

Personally, I don’t think the movement is 100% accurate yet, but the current settings are a good starting point.

## Notes:

### Coordinate System
Quake uses a right-handed coordinate system while Unity uses a left-handed one. So coordinate values (X,Y,Z) have been swapped to reflect this difference.

### World Scale
UPS (units per second) is measured in Unity units (meters) and not idTech units.

### Configuration
Elastic movement configuration — you can enable selected behaviours and set value ranges for friction, gravity, and other parameters.

### Demo Assets
Demo scene meshes were built with ProBuilder, so this package must be installed in your project for the demo scene to function correctly.
