# Werking Applicatie en Acceptatietest

Ik ben bijna klaar met de smart energy applicatie. In deze blogpost vertel ik hoe de applicatie werkt, welke keuzes ik heb gemaakt, en of het algoritme voldoet aan de gestelde eisen.

## Inputdata: Wat heb ik nodig?

Mijn applicatie verwacht drie inputs:

- **MeterID**: De unieke identificatie van jouw slimme meter. Hiermee haal ik de juiste data uit InfluxDB en kun je verschillende huishoudens analyseren.
- **Dagen**: Het aantal dagen waarvoor je data wilt ophalen (minimaal 1, maximaal 90).
- **Vaste tarief**: De prijs voor vergelijking met het dynamische tarief in €/kWh (standaard €0.25).

![Input formulier](./input_fields_form.png "Inputvelden voor de analyse")

De applicatie haalt vier soorten data op, maar ik gebruik vooral de **vermogensdata** omdat deze per uur laat zien hoeveel Watt er werd verbruikt. Door dit te combineren met dynamische prijzen kan ik exact berekenen wat het kost. Mijn verwachting was dat dynamisch goedkoper zou zijn omdat je kunt besparen door stroom te gebruiken wanneer het goedkoop is.

## Algoritme-ontwerp: Hoe werkt het?

![flowchart](./flowchart_high_level.png "Flowchart van het algoritme")

Mijn algoritme volgt deze stappen:

1. **Data ophalen** met "1h" aggregatie (één gemiddelde per uur)
2. **Eenheid detecteren**: Als het gemiddelde boven 100 ligt, is het in Watts – dan deel ik door 1000 om naar kilowatts te converteren
3. **Groeperen per dag** met LINQ
4. **Energie berekenen**: Vermogen × Tijd = Energie (500W × 1 uur = 0.5 kWh)
5. **Kosten berekenen**: Dynamisch gebruikt elk uur zijn specifieke prijs, vast gebruikt altijd dezelfde prijs
6. **Totalen optellen** en besparing berekenen

Deze aanpak klopt fysisch en gebruikt de échte dynamische prijzen uit de database.

## Resultaten: Wat zie je?

De applicatie toont drie soorten resultaten:

**Tabellen** met de ruwe data. Hier zie je de eerste 10 regels van vermogen en gas:

![Data tabellen](./power_gas_table_output.png "Power en Gas tabellen met eerste 10 metingen")

Je ziet direct de Watt-waarden per uur en de bijbehorende energieprijzen. Bijvoorbeeld op 2025-09-29 om 06:00 was de prijs €0.234/kWh (piekuur!), terwijl om 02:00 het slechts €0.098/kWh was.

**Grafieken** voor visuele analyse:

![Cost Analysis Charts](./cost_analysis_charts.png "Lijngrafiek kostenvergelijking en staafdiagram dagelijks verbruik")

De lijngrafiek toont duidelijk dat het vaste tarief (groene lijn) consistent boven het dynamische tarief (blauwe lijn) ligt – dit betekent dat dynamisch goedkoper is. Het staafdiagram laat zien dat het dagelijks verbruik varieert, met pieken rond de 30 kWh op sommige dagen.

**Kosten-overzicht** met de eindcijfers:

![Total Cost Summary](./total_cost_summary.png "Totale kosten en besparingen")

Voor 30 dagen zie je: €44.53 met dynamisch, €95.17 met vast tarief – een besparing van €50.64 (53.2%!).

Wat opviel: deze besparingen van 53% zijn veel hoger dan mijn verwachte 10-20%. Dit komt omdat ik 30 dagen data heb gebruikt met grote prijsverschillen tussen dag en nacht. Aanvankelijk waren de kosten in de *miljoenen* – dat was een Watts/kilowatts bug die ik vond door sanity checks. Ook zag ik dat dynamisch niet altijd goedkoper is: op dagen met veel verbruik tijdens piekuren (zoals te zien om 06:00 met €0.234/kWh) kan vast voordeliger zijn.

## Acceptatiecriteria en testen

De opdracht vroeg om een webpagina met naam, algoritme-beschrijving, resultaten, dagelijkse berekeningen, vergelijking dynamisch vs. vast, inputveld voor vaste prijs, en totalen. Dit heb ik allemaal gerealiseerd, plus extra's zoals grafieken en multi-huishouden support.

Ik testte door handmatige verificatie (24 metingen narekenen), sanity checks (waarschuwingen bij onrealistisch verbruik), en variërende inputs (1, 7, 30 dagen met verschillende prijzen). Alles werkte correct. Onverwachte bevinding: dynamisch is niet automatisch altijd goedkoper – het hangt af van je verbruikspatroon.

## Reflectie en verbetering

Als ik het opnieuw zou doen: flowchart eerder maken, beter testen met edge cases (Meter ID 0?), "top 5 goedkoopste uren" toevoegen, en meer comments bij LINQ-queries.

Desondanks ben ik tevreden – het algoritme werkt, de UI is professioneel, en alle eisen zijn behaald.

## Terugkijkend: Lost dit het probleem op?

In mijn eerste blogpost noemde ik drie SDG's: **SDG 7.3** (energie-efficiëntie door inzicht in verbruik), **SDG 11.6** (milieu-impact door verbruiksverschuiving), en **SDG 13.2** (klimaatmaatregelen door hernieuwbare energie beter te integreren).

Draagt mijn applicatie hieraan bij? **Ja, deels.** Het is een belangrijke eerste stap: bewustwording. De grafieken tonen duidelijk wanneer stroom goedkoop is (vaak als er veel zon/wind is), wat mensen helpt bewuster te kiezen wanneer ze apparaten aanzetten.

Privacy heb ik gerespecteerd: data wordt alleen geaggregeerd per dag getoond. Kortom: de applicatie helpt mensen bewuster omgaan met energie en draagt bij aan SDG 7, 11 en 13. Het is geen volledige oplossing, maar een solide fundament.
