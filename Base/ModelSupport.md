# Überblick

Das Objekt-Modell für die grafische Darstellung

# Objekte

* Displayable - die abstrakte Oberklasse für alle anzeigbaren Objekte
* Surface - Die Komposit-Klasse für die Darstellung des Objekts bestehend aus:
    * Shape - Die Form des Objekts, entweder geometrisch(Körper) oder willkürlich (Punktmenge)
    * Material - Oberflächenbeschaffenheit und Lichtsituation eines Objekts
    * Textur - Eine vorgefertigte Detailstruktur der Oberfläche

# Objektstruktur

Ein Displayble kann aus mehreren Teilen bestehen, welches jeweil eigene Eigenschaften in Bezug
auf die Form, Material und Textur haben.

