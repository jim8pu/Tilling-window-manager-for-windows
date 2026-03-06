# Start
# SeelenWM Logic Flowchart

Here is the visual breakdown of the "Universal Logic" we built.

> [!NOTE]
> This flowchart uses quotes around labels to prevent parsing errors.

```mermaid
flowchart TD
    Start([New Window Detected]) --> Visible{"Is Visible?"}
    Visible -- No --> Ignore([Ignore Window])
    Visible -- Yes --> Cloaked{"Is Cloaked?"}
    Cloaked -- Yes --> Ignore
    Cloaked -- No --> Config{"User Config Rule?"}
    
    Config -- Ignore --> Ignore
    Config -- Force --> Tile([Tile Window])
    Config -- None --> DialogCheck{"Universal Dialog Check"}
    
    subgraph "Universal Dialog Logic"
        DialogCheck --> ClassCheck{"Is Dialog Class?"}
        ClassCheck -- "Yes (#32770)" --> Ignore
        ClassCheck -- No --> OwnerCheck{"Has Owner?"}
        OwnerCheck -- Yes --> Ignore
        OwnerCheck -- No --> FeatureCheck{"Can Maximize?"}
        
        FeatureCheck -- No --> Ignore
        FeatureCheck -- Yes --> ToolWindow{"Is ToolWindow?"}
        ToolWindow -- Yes --> Ignore
        
        ToolWindow -- No --> Tile
    end

    Tile --> BorderLogic[Apply Focus Border]
    
    subgraph "Smart Border Logic"
        BorderLogic --> GetRect["Get Window Rect (Physical)"]
        GetRect --> CalcPos[Calculate Border Position]
        CalcPos --> SetZ["Set Z-Order: Just Above Window"]
        SetZ --> Draw[Draw Blue Border]
        
        style SetZ fill:#d4f1f9,stroke:#0078d7,stroke-width:2px
    end
    
    Ignore --> End([Done])
    Draw --> End
```

## Key Decisions

1.  **Can Maximize?**
    -   This is the "Golden Rule".
    -   **Apps** (Chrome, VS Code, Notepad) let you maximize them to fill the screen.
    -   **Dialogs** (Folder In Use, Settings, Popups) usually have a fixed size and *cannot* be maximized.
    -   By checking this, we filter out 99% of annoyance windows without knowing their names!

2.  **Smart Z-Order**
    -   Instead of forcing the Blue Border to be "Always On Top" (which covers everything), we tell Windows: *"Put this border right on top of the target window, but let other topmost windows (like QuickLook) sit above it."*
