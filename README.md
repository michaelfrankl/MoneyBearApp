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
![Dashboard_Bild_1](https://github.com/user-attachments/assets/2a5ba8db-0034-4fd0-9eff-a559b4f932f0)

Dashboard - Einnahmen/Ausgaben Farben anpassen
![Dashboard_Bild_2](https://github.com/user-attachments/assets/1ce65c4d-274a-4483-8cbb-834c2c779ef7)

Dashboard - Ausgaben nach Kategorie Farben anpassen
![Dashboard_Bild_3](https://github.com/user-attachments/assets/5cf948f2-886c-413f-a305-604fa2680a90)

Dashboard - Spezielle Posten anpassen
![Dashbaord_Bild_4](https://github.com/user-attachments/assets/2c03397c-6004-4d52-bb2d-4f5aae2edaf2)

## Gläubiger ##
Hier können alle Ausgaben und Einnahmen erfasst werden, welche unregelmäßig sind. Beispielweise, wenn jemand das Tanken übernimmt oder man jemandem Geld 
leiht. Dies kann hier genau erfasst werden, ggf. sogar mit einer entsprechenden Notiz sowie einem Bild
(Aktuell noch im Ausbau, es werden noch Filter hinzugefügt wie z.B. für die Art der Transaktion sowie den PDF-Export nach bezahlt/!bezahlt)

Gläubiger - Übersicht, sowie Bearbeitung
![Debts_1](https://github.com/user-attachments/assets/74b2dcb4-5a87-422b-8bf9-4b651fa2ed72)

Gläubiger - Neuer Eintrag
![Debts_2](https://github.com/user-attachments/assets/8ebfe37d-4c3c-4634-a526-fd693391709d)

## Grid ##
Das Kernelement der Anwendung. Hier ist es möglich, alle Zeilen in der Übersicht von einem Jahr darzustellen. Oder + Anzahl Monate zum Jahresende.
Zeilen bzw. Zellen können jeweils mit einem Kommentar belegt werden. Alle Werte können durch den automatischen Import hinzugefügt werden, mit einer jeweiligen
Frequenz von 1 bis 12 Monaten. Sowie die Möglichkeit, eigene Summen bilden zu können, um eine einfache Übersicht bei vielen Posten zu erstellen (siehe Bild 5).

Grid - Darstellung mit Kommentar
![Grid_Bild_1](https://github.com/user-attachments/assets/0e909476-3447-4e86-be69-c0cb8764398e)

Grid - Übersicht von März bis Dezember (+9 Monate, max +12)
![Grid_Bild_2](https://github.com/user-attachments/assets/695e3dec-5d5a-4f56-b3f3-e6cc1d244bf0)

Grid - Zeile bearbeiten
![Grid_Bild_3](https://github.com/user-attachments/assets/5db888af-5808-4465-9447-c964c7fe8bc5)

Grid - Zeile hinzufügen
![Grid_Bild_4](https://github.com/user-attachments/assets/d042b09b-3fbe-42c9-ace2-3b46263e82b7)

Grid - Summenzeile hinzufügen mit Berechnung
![Grid_Bild_5](https://github.com/user-attachments/assets/ed2474eb-5eaf-461b-98c2-f736ff911294)

Beim Import werden die jeweiligen Werte genommen, welche beim Erstellen der Zeile gesetzt wurden bzw. bei den Importeinstellungen eingetragen wurden
(siehe Bild 4)

Grid - Import Monatsauswahl
![Grid_Import_1](https://github.com/user-attachments/assets/9179dec9-9f21-4d24-abd7-71cdeb7b4db4)

Grid - Definierte Zeilen importieren
![Grid_Import_2](https://github.com/user-attachments/assets/3297131a-3a9c-4fcf-b777-264c372a0f4b)

Grid - Kontostand importieren
![Grid_Import_3](https://github.com/user-attachments/assets/30ca506e-a689-44e9-a979-bdd0e87a4587)

Grid - Importwerte anpassen (Einstellungen)
![Grid_Import_4](https://github.com/user-attachments/assets/7cab62c1-c1ba-49b0-aaea-a289da0c1fd7)

## Settings ##
Bei den Settings sind aktuell die Importwerte sowie die Benutzerverwaltung erfasst. Weiteres folgt.

Settings - Neuen Benutzer anlegen
![Settings_1](https://github.com/user-attachments/assets/54bef053-8b30-4211-91cb-eddf8b789c53)

Settings - Benutzer bearbeiten
** Benutzer mit der Administrator-Rolle können Nutzerdaten sowie Rollen anpassen, aber das jeweilige Passwort bleibt verschlüsselt.
![Settings_2](https://github.com/user-attachments/assets/5731e3b7-cc7b-4356-aeb9-a9e92f3d1ad4)

Settings - Anmeldung
** Da die Datenbank aktuell lokal gehostet wird, ist das IPv4-Feld leer. Wenn dies z.B. zu Azure gewechselt wird, ist
hier eine Möglichkeit der einfachen Darstellung ggf. mit einem Button (sichtbar/unsichtbar) machen.
![Settings_3](https://github.com/user-attachments/assets/85112a73-7787-4f82-8b48-f905d2813bae)

** Geplante Erweiterungen **
- Prognose über bestimmte Kategorien, d.h. wie verhalten sich manche Posten über das Jahr (Hoch- und Tiefwerte)
- Investitionsthemen erfassen und Darstellung ggf. mit Live-Kurs-Ansicht
- Export vom Grid nach Excel mit jeweiligen Summen
- Export zu PDF (Alle relevanten Daten mit Filtermöglichkeiten)




