# ****OSK Framework Overview****

The **OSK Framework** is a modular, high-performance Unity framework designed to streamline game development. It provides a robust suite of tools to manage core game systems like events, pooling, sound, and UI, ensuring scalability and maintainability for professional projects.

**version 3.5.0 (Current)
- **Performance Overhaul**: Integrated a centralized high-performance tick system (`IUpdateable`).
- **Object Pooling 2.0**: O(1) performance optimization using `Stack` and `HashSet` containers.
- **Workflow Improvement**: Added "Sync Modules" button for one-click hierarchy setup.
- **Smart Lifecycle**: Automated tick registration/unregistration for pooled objects.
- **Enhanced Debugging**: Centralized tick lists visible in the `Main` inspector.

**version 3.1
- Update debug, Add SheetDataManager
- Fixbug

**version 3.0
- Update Editor UI, Sound SO
- Fixbug

**version 2.5.0
- Remove UIParticle, Timer, ROP
- Update EventBus, Resource, Singleton
- Add Dependency

**version 2.4.0
- Remove State, DI, Network, Native GameFrameworkComponent
- Fixbug

**version 2.3.0
- Remove SO Config and Module creation on Main
- Fixbug sound, ui

**version 2.2.0
- Add auto bind Refs UI
- Add set/get value default SoundDataSO

---

## **🌟 Key Features**

**Source Link**: [OSK Framework Core](https://github.com/O-S-K/Osk-Framework/tree/main/Runtime/Scripts/Core)

1. [**Centralized Tick System**]: High-performance replacement for Unity's `Update/FixedUpdate` loops via `IUpdateable` interfaces.
2. [**PoolManager**]: Optimized O(1) object pooling with smart despawn (supports GO, Transform, and Components).
3. [**EventBusManager**]: Decoupled event broadcasting and subscription system.
4. [**InputDeviceManager**]: Unified input handling integrated into the centralized tick system.
5. [**SoundManager**]: Comprehensive control for BGM, SFX, and audio events with SO-based configuration.
6. [**UIManager**]: Advanced UI management with screen transitions, pooling, and auto-binding refs.
7. [**BlackboardManager**]: Shared data storage for AI, state machines, and gameplay logic.
8. [**ProcedureManager**]: Structured FSM-based workflow for game states and initialization.
9. [**ResourceManager**]: Efficient loading, caching, and unloading of Unity assets.
10. [**DataManager**]: Handles both runtime data and persistent encrypted storage.
11. [**LocalizationManager**]: Robust multi-language support.
12. [**EntityManager**]: Lightweight management for game entity lifecycles.
13. [**WebRequestManager**]: Simplified HTTP request handling.
14. [**SheetDataManager**]: Automatic loading of SO-based data sheets from Resources.
15. [**ObserverManager**]: Traditional observer pattern for direct event communication.
16. [**CommandManager**]: Supports the Command pattern for undo/redo and input recording.
17. [**DirectorManager**]: Scene management and smooth transition control.

---

## **🚀 Quick Start**

### **1. Install Dependencies**
- **Odin Inspector**: [Required for Editor UI] (https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041)
- **DoTween**: [O-S-K Fork] (https://github.com/O-S-K/DOTween)
- **Newtonsoft.json**: `com.unity.nuget.newtonsoft-json`
- **UniTask**: [High-perf Async] (https://github.com/Cysharp/UniTask)

### **2. Setup Framework**
1. Navigate to **Window → OSK-Framework → CreateFramework** to initialize the `Main` singleton in your scene.
2. In the `Main` Inspector, go to **Main Modules** and activate the features your project needs.
3. Click the **"Sync Modules (Hierarchy)"** button to automatically generate and configure module GameObjects in your scene hierarchy.

### **3. Performance Best Practices**
- **Use `IUpdateable`**: Instead of `void Update()`, implement `IUpdateable` and register via `Main.RegisterTick(this)`. This avoids Unity's native message overhead.
- **Pooling**: Use `Main.Pool.Spawn<T>()` and `Main.Pool.Despawn(this)` for high-frequency objects to eliminate GC Alloc.

### **4. Accessing Systems**
```csharp
// Example Usage
Main.Pool.Spawn(prefab);        // Pooling
Main.UI.Open<HomeView>();       // UI
Main.Sound.Play("Click");       // Sound
Main.Event.Publish(new ScoreChangedEvent()); // Events
```
  
---

## **🎯 Why OSK Framework?**
- **Ultra-Fast**: Designed for 60/120 FPS games with zero GC spikes in core loops.
- **Modular**: Only pay for the features you use.
- **Workflow-First**: Deep integration with Odin Inspector for a premium developer experience.

---

## **📞 Support**
- **Email**: gamecoding1999@gmail.com  
- **Facebook**: [OSK Framework Community](https://www.facebook.com/xOskx/)
