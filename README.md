![GitHub-License](https://img.shields.io/badge/License-MIT-51%2C163%2C63?logo=github)

# MoneyBear - Haushaltsbuch bzw. Finanzplaner #

Die Anwendung soll es ermöglichen, die eigenen Ausgaben und Einnahmen über einen längeren Zeitraum einfach und effektiv zu beobachten.
Aktuell befindet sich die Anwendung noch in Entwicklung und ist nicht fertiggestellt.
Der zentrale Punkt der Anwendung ist die einfache Erfassung sowie Darstellung der jeweiligen Werte.

Aktuell gibt es drei zentrale Punkte + Settings
- Dashboard
- Gläubiger
- Grid

* Bei der Darstellung im Weiteren wurde mit wenigen Datensätzen das Ganze veranschaulicht.

## Dashboard ##
Im Dashboard werden alle Ein- und Ausgaben grafisch dargestellt und farblich anpassbar. Des Weiteren ist es hier möglich, spezielle Posten 
immer anzeigen zu lassen, siehe Bild 2-4.

Dashboard Übersicht
![Dashboard_Bild_1](https://github.com/user-attachments/assets/ed84d071-136c-4182-9f20-cf799a46a2cd)

Dashboard - Einnahmen/Ausgaben Farben anpassen
![Dashboard_Bild_2](https://github.com/user-attachments/assets/c1ef4f2e-927a-4550-ac56-6daee019ead5)

Dashboard - Ausgaben nach Kategorie Farben anpassen
![Dashboard_Bild_3](https://github.com/user-attachments/assets/da5359fc-5a86-4c9f-9d5c-d46c777e1ac6)

Dashboard - Spezielle Posten anpassen
![Dashbaord_Bild_4](https://github.com/user-attachments/assets/b737296a-f611-40b0-ac3b-7930fe331ddc)

## Gläubiger ##
Hier können alle Ausgaben und Einnahmen erfasst werden, welche unregelmäßig sind. Beispielweise, wenn jemand das Tanken übernimmt oder man jemandem Geld 
leiht. Dies kann hier genau erfasst werden, ggf. sogar mit einer entsprechenden Notiz sowie einem Bild
(Aktuell noch im Ausbau, es werden noch Filter hinzugefügt wie z.B. für die Art der Transaktion sowie den PDF-Export nach bezahlt/!bezahlt)

Gläubiger - Übersicht, sowie Bearbeitung
![Debts_1](https://github.com/user-attachments/assets/e18ccf18-178e-40e3-ae59-acebc409150d)

Gläubiger - Neuer Eintrag
![Debts_2](https://github.com/user-attachments/assets/ab54c374-60bf-4174-9f60-b2ee5aa2a277)

## Grid ##
Das Kernelement der Anwendung. Hier ist es möglich, alle Zeilen in der Übersicht von einem Jahr darzustellen. Oder + Anzahl Monate zum Jahresende.
Zeilen bzw. Zellen können jeweils mit einem Kommentar belegt werden. Alle Werte können durch den automatischen Import hinzugefügt werden, mit einer jeweiligen
Frequenz von 1 bis 12 Monaten. Sowie die Möglichkeit, eigene Summen bilden zu können, um eine einfache Übersicht bei vielen Posten zu erstellen (siehe Bild 5).

Grid - Darstellung mit Kommentar
![Grid_Bild_1](https://github.com/user-attachments/assets/e3cdf027-53a0-401b-8b91-aafba459b243)

Grid - Übersicht von März bis Dezember (+9 Monate, max +12)
![Grid_Bild_2](https://github.com/user-attachments/assets/a7b395dd-a778-4b87-9a18-d0a130145bba)

Grid - Zeile bearbeiten
![Grid_Bild_3](https://github.com/user-attachments/assets/7cac7997-09a3-40fa-abd6-38d66620f1b2)

Grid - Zeile hinzufügen
![Grid_Bild_4](https://github.com/user-attachments/assets/f3f8fd22-4f08-45fa-9c85-ca2418880747)

Grid - Summenzeile hinzufügen mit Berechnung
![Grid_Bild_5](https://github.com/user-attachments/assets/6b2b9f4f-2a88-4f1f-851b-5885d9e0740f)

Beim Import werden die jeweiligen Werte genommen, welche beim Erstellen der Zeile gesetzt wurden bzw. bei den Importeinstellungen eingetragen wurden
(siehe Bild 4)

Grid - Import Monatsauswahl
![Grid_Import_1](https://github.com/user-attachments/assets/d37dd1e7-a503-434a-bab8-9d37cb448881)

Grid - Definierte Zeilen importieren
![Grid_Import_2](https://github.com/user-attachments/assets/236f9a9a-a8dc-416d-83b9-1b7ebd043b92)

Grid - Kontostand importieren
![Grid_Import_3](https://github.com/user-attachments/assets/806f737c-7798-4430-8188-ee30982c1917)

Grid - Importwerte anpassen (Einstellungen)
![Grid_Import_4](https://github.com/user-attachments/assets/b669883e-ec91-4144-b798-c8dba46e7edb)

## Settings ##
Bei den Settings sind aktuell die Importwerte sowie die Benutzerverwaltung erfasst. Weiteres folgt.

Settings - Neuen Benutzer anlegen
![Settings_1](https://github.com/user-attachments/assets/e8384e31-1a93-434b-aadf-c1f368e0e94b)

Settings - Benutzer bearbeiten
** Benutzer mit der Administrator-Rolle können Nutzerdaten sowie Rollen anpassen, aber das jeweilige Passwort bleibt verschlüsselt.
![Settings_2](https://github.com/user-attachments/assets/a8235191-5f00-4b8a-bda5-6089e1de96e2)

Settings - Anmeldung
** Da die Datenbank aktuell lokal gehostet wird, ist das IPv4-Feld leer. Wenn dies z.B. zu Azure gewechselt wird, ist
hier eine Möglichkeit der einfachen Darstellung ggf. mit einem Button (sichtbar/unsichtbar) machen.
![Settings_3](https://github.com/user-attachments/assets/666289bb-99e3-4c32-9b75-72791ef9d049)

** Geplante Erweiterungen **
- Prognose über bestimmte Kategorien, d.h. wie verhalten sich manche Posten über das Jahr (Hoch- und Tiefwerte)
- Investitionsthemen erfassen und Darstellung ggf. mit Live-Kurs-Ansicht
- Export vom Grid nach Excel mit jeweiligen Summen
- Export zu PDF (Alle relevanten Daten mit Filtermöglichkeiten)




