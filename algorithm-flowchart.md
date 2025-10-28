# Smart Energy Cost Analysis - Algorithm Flowchart

## High-Level Application Flow

```mermaid
flowchart TD
    Start([User Opens /assignment Page]) --> Init[Initialize Component<br/>OnInitialized]
    Init --> CreateForm[Create Form with Defaults<br/>MeterId=1, Days=1, Price=€0.25]
    CreateForm --> RenderPage[Render Page with Form]
    RenderPage --> WaitInput[Wait for User Input]
    
    WaitInput --> UserFills[User Fills Form:<br/>- Meter ID<br/>- Days to Retrieve<br/>- Fixed Price]
    UserFills --> UserSubmits[User Clicks 'Load Data']
    
    UserSubmits --> Validate{Form Valid?}
    Validate -->|No| RenderPage
    Validate -->|Yes| HandleSubmit[HandleValidSubmit<br/>Method Called]
    
    HandleSubmit --> LoadData[Load Data from InfluxDB]
    LoadData --> LoadEnergy[Get Energy Consumed<br/>hourly, 1h aggregation]
    LoadData --> LoadProduced[Get Energy Produced<br/>hourly, 1h aggregation]
    LoadData --> LoadPower[Get Power Data<br/>hourly, 1h aggregation]
    LoadData --> LoadGas[Get Gas Delivered<br/>hourly, 1h aggregation]
    
    LoadPower --> CheckData{Power Data<br/>Available?}
    CheckData -->|No| ShowError[Show 'No Data' Message]
    CheckData -->|Yes| PerformAnalysis[PerformCostAnalysis<br/>Method Called]
    
    PerformAnalysis --> DetectUnits[Detect if Data is<br/>Watts or Kilowatts]
    DetectUnits --> GroupByDay[Group Measurements<br/>by Day using LINQ]
    GroupByDay --> CalcDaily[Calculate Per Day:<br/>- Energy Consumed kWh<br/>- Dynamic Cost<br/>- Fixed Cost]
    CalcDaily --> CalcTotals[Calculate Totals:<br/>- Total Dynamic Cost<br/>- Total Fixed Cost<br/>- Savings<br/>- Savings %]
    
    CalcTotals --> UpdateUI[Update UI with Results]
    UpdateUI --> ShowTables[Display Data Tables<br/>First 10 Records]
    UpdateUI --> ShowCharts[Render ApexCharts<br/>- Line Chart Cost Comparison<br/>- Bar Chart Consumption]
    UpdateUI --> ShowSummary[Display Cost Summary<br/>4 Key Metrics]
    
    ShowSummary --> End([User Views Results])
    
    style Start fill:#90EE90
    style End fill:#90EE90
    style PerformAnalysis fill:#FFD700
    style HandleSubmit fill:#FFD700
    style ShowCharts fill:#87CEEB
    style ShowSummary fill:#87CEEB
```

## Detailed Algorithm Flow - PerformCostAnalysis

```mermaid
flowchart TD
    Start([PerformCostAnalysis<br/>Method Called]) --> CheckNull{PowerData<br/>null or empty?}
    
    CheckNull -->|Yes| LogError[Log: No power data available]
    LogError --> ReturnEarly[Return Early]
    
    CheckNull -->|No| LogDebug[Log Sample Data<br/>First 10 Measurements]
    
    LogDebug --> CalcAvg[Calculate Average<br/>Power Value]
    CalcAvg --> DetectUnit{Average > 100?}
    
    DetectUnit -->|Yes| SetWatts[isWatts = true<br/>powerToKW = 0.001]
    DetectUnit -->|No| SetKW[isWatts = false<br/>powerToKW = 1.0]
    
    SetWatts --> LogUnit[Log Detected Unit]
    SetKW --> LogUnit
    
    LogUnit --> StartLINQ[Start LINQ Query]
    
    StartLINQ --> GroupData[GroupBy Timestamp.Date<br/>Groups all hours by day]
    
    GroupData --> SelectTransform[Select Transform<br/>For Each Day Group:]
    
    SelectTransform --> CalcEnergy[EnergyConsumedKWh =<br/>Sum Value × powerToKW<br/>Physics: Power × Time = Energy]
    
    CalcEnergy --> CalcDynamic[DynamicCost =<br/>Sum Value × powerToKW × EnergyPrice<br/>Uses hourly varying prices]
    
    CalcDynamic --> CalcFixed[FixedCost =<br/>Sum Value × powerToKW × FixedPrice<br/>Uses constant price]
    
    CalcFixed --> OrderData[OrderBy Date<br/>Sort Chronologically]
    
    OrderData --> Materialize[ToList<br/>Execute Query]
    
    Materialize --> SumDynamic[TotalDynamicCost =<br/>Sum all DynamicCost]
    
    SumDynamic --> SumFixed[TotalFixedCost =<br/>Sum all FixedCost]
    
    SumFixed --> CalcSavings[TotalSavings =<br/>FixedCost - DynamicCost]
    
    CalcSavings --> CalcPercent[SavingsPercentage =<br/>Savings / FixedCost × 100]
    
    CalcPercent --> LogResults[Log Detailed Results:<br/>- Per Day Statistics<br/>- Min/Max Power<br/>- Total Costs<br/>- Average Daily kWh]
    
    LogResults --> SanityCheck{Total kWh > 1000<br/>or Daily Avg > 100?}
    
    SanityCheck -->|Yes| LogWarning[Log Warning:<br/>Consumption seems high]
    SanityCheck -->|No| Done
    
    LogWarning --> Done([Analysis Complete<br/>Data Ready for Display])
    
    style Start fill:#FFD700
    style Done fill:#90EE90
    style DetectUnit fill:#FFA500
    style SanityCheck fill:#FFA500
    style GroupData fill:#87CEEB
    style CalcEnergy fill:#87CEEB
    style CalcDynamic fill:#87CEEB
    style CalcFixed fill:#87CEEB
```

## Data Flow Diagram

```mermaid
flowchart LR
    subgraph Input
        User[User Input:<br/>MeterId<br/>Days<br/>FixedPrice]
    end
    
    subgraph Database
        InfluxDB[(InfluxDB<br/>Time-Series Data)]
    end
    
    subgraph Processing
        Load[Load Hourly<br/>Measurements]
        Detect[Detect<br/>W vs kW]
        Group[Group by<br/>Day]
        Calc[Calculate<br/>Costs]
    end
    
    subgraph Output
        Tables[Data Tables<br/>Raw Values]
        LineChart[Line Chart<br/>Cost Comparison]
        BarChart[Bar Chart<br/>Daily Consumption]
        Summary[Summary Cards<br/>Total Costs]
    end
    
    User --> Load
    InfluxDB --> Load
    Load --> Detect
    Detect --> Group
    Group --> Calc
    Calc --> Tables
    Calc --> LineChart
    Calc --> BarChart
    Calc --> Summary
    
    style User fill:#90EE90
    style InfluxDB fill:#FFD700
    style Calc fill:#FFA500
    style Summary fill:#87CEEB
```

## Key Algorithm Steps Explained

### Step 1: Unit Detection Logic
```mermaid
flowchart TD
    Start[Get Power Data] --> Avg[Calculate Average<br/>of all Values]
    Avg --> Check{Average > 100?}
    Check -->|Yes| Watts[Data is in Watts W<br/>Conversion: ÷ 1000]
    Check -->|No| KW[Data is in Kilowatts kW<br/>Conversion: × 1]
    
    Watts --> Example1[Example: 500W × 0.001 = 0.5 kW]
    KW --> Example2[Example: 0.5 kW × 1 = 0.5 kW]
    
    Example1 --> Use[Use in Calculations]
    Example2 --> Use
    
    style Check fill:#FFA500
```

### Step 2: Energy Calculation (Physics)
```mermaid
flowchart TD
    Start[Hourly Power Data] --> Formula[Power × Time = Energy]
    Formula --> Example[500W × 1 hour = 500 Wh]
    Example --> Convert[500 Wh ÷ 1000 = 0.5 kWh]
    Convert --> Day[Sum All Hours in Day]
    Day --> Result[Total Daily Energy kWh]
    
    style Formula fill:#FFD700
```

### Step 3: Cost Calculation
```mermaid
flowchart TD
    Start[Energy per Hour kWh] --> Split{Calculate Both:}
    
    Split --> Dynamic[Dynamic Cost:<br/>Energy × Hourly Price]
    Split --> Fixed[Fixed Cost:<br/>Energy × Fixed Price]
    
    Dynamic --> DynEx[Example:<br/>0.5 kWh × €0.15 = €0.075]
    Fixed --> FixEx[Example:<br/>0.5 kWh × €0.25 = €0.125]
    
    DynEx --> SumDyn[Sum All Hours<br/>in Period]
    FixEx --> SumFix[Sum All Hours<br/>in Period]
    
    SumDyn --> Compare[Compare Totals]
    SumFix --> Compare
    
    Compare --> Savings[Calculate Savings:<br/>Fixed - Dynamic]
    
    style Compare fill:#90EE90
```

## LINQ GroupBy Visualization

```mermaid
flowchart TD
    subgraph Before [Before GroupBy - Hourly Data]
        H1[10/28 00:00 - 500W]
        H2[10/28 01:00 - 450W]
        H3[10/28 02:00 - 600W]
        H4[10/29 00:00 - 550W]
        H5[10/29 01:00 - 480W]
    end
    
    subgraph GroupOperation [GroupBy Timestamp.Date]
        Group[Group by Date Only<br/>Ignore Time]
    end
    
    subgraph After [After GroupBy - Daily Groups]
        Day1[10/28 Group<br/>500W, 450W, 600W]
        Day2[10/29 Group<br/>550W, 480W]
    end
    
    subgraph Select [Select Transform]
        Transform[For Each Group:<br/>Calculate Totals]
    end
    
    subgraph Result [Final Result]
        R1[10/28: 1.55 kWh, €0.27]
        R2[10/29: 1.03 kWh, €0.18]
    end
    
    H1 --> Group
    H2 --> Group
    H3 --> Group
    H4 --> Group
    H5 --> Group
    
    Group --> Day1
    Group --> Day2
    
    Day1 --> Transform
    Day2 --> Transform
    
    Transform --> R1
    Transform --> R2
    
    style Group fill:#FFD700
    style Transform fill:#FFA500
```

---

## How to View These Flowcharts

### Option 1: GitHub/GitLab (Automatic Rendering)
- Push this file to GitHub/GitLab
- They automatically render Mermaid diagrams
- Click the file to view

### Option 2: VS Code Extension
1. Install "Markdown Preview Mermaid Support" extension
2. Open this file in VS Code
3. Press `Ctrl+Shift+V` (Windows) or `Cmd+Shift+V` (Mac)
4. View rendered diagrams

### Option 3: Online Editor
1. Go to https://mermaid.live/
2. Copy-paste any diagram code
3. View and export as PNG/SVG

### Option 4: Obsidian
- Obsidian has built-in Mermaid support
- Just open this file in Obsidian

---

## Summary

This algorithm:
1. **Loads** hourly power consumption data from InfluxDB
2. **Detects** whether data is in Watts or Kilowatts
3. **Groups** measurements by day using LINQ
4. **Calculates** daily energy consumption (kWh)
5. **Compares** costs between dynamic and fixed tariffs
6. **Visualizes** results with charts and summary statistics
7. **Validates** results with sanity checks

**Core Formula:** Cost = Energy (kWh) × Price (€/kWh)

**Key Insight:** Dynamic tariffs save money when you consume energy during cheaper hours.
