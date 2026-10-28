### Consignes .NET: 
* Ignorez les migrations BDD
* Ne pas modifier les classes qui ont un commentaire: `// WARN: Should not be changed during the exercise`
* Pour lancer les tests: `dotnet test`

---

## Refactoring

### Démarche

1. **Filet de sécurité d'abord** : des tests d'intégration de caractérisation ont été écrits et validés sur le code
   d'origine (commit `test: add characterization tests…`). Ils figent le comportement de chaque type de produit
   (stock + notifications) avant toute modification.
2. **Extraction des règles métier** : une classe par type de produit (pattern Strategy).
3. **Séparation des couches** : contrôleur → service applicatif → repository.
4. Chaque étape fait l'objet d'un commit dédié : `git log --oneline` retrace la démarche.

### Architecture

```
Controllers/OrdersController            Présentation : HTTP uniquement (200 / 404)
        │
Services/Orders/OrderService            Application : charge la commande, délègue chaque produit, sauvegarde une fois
        │                    └── IOrderRepository (port)
Services/Products/*ProductHandler       Règles métier, une classe par type (NORMAL, SEASONAL, EXPIRABLE)
        │
Database/Repositories/OrderRepository   Persistance : seul point d'accès à Entity Framework
```

| Type | Règle | Classe |
|---|---|---|
| `NORMAL` | En stock → décrément. Sinon, délai annoncé si `LeadTime > 0`. | `NormalProductHandler` |
| `SEASONAL` | En saison et en stock → décrément. Sinon : réappro après la fin de saison → indisponible ; saison pas commencée → indisponible ; sinon délai annoncé. | `SeasonalProductHandler` |
| `EXPIRABLE` | Non expiré et en stock → décrément. Sinon → notification d'expiration, stock à 0. | `ExpirableProductHandler` |

L'horloge est injectée (`IDateTimeProvider`) pour tester les règles de dates de façon déterministe.

### Tests

| Fichier | Portée |
|---|---|
| `Controllers/OrdersControllerIntegrationTests` | API de bout en bout : tous les types, cas limites, commande inexistante |
| `Services/Orders/OrderServiceTests` | Aiguillage par type, type inconnu ignoré, sauvegarde unique |
| `Services/Products/*HandlerTests` | Chaque règle métier, y compris les bornes de dates |

### Changement de comportement assumé

* Une commande inexistante renvoie **404 Not Found** au lieu de planter (NullReferenceException → 500).

Tout le reste du comportement est conservé à l'identique, y compris les particularités ci-dessous.

### Points identifiés, conservés volontairement (à valider avec le métier)

* Un produit `EXPIRABLE` en rupture de stock mais non expiré déclenche une notification **d'expiration**.
* Les bornes de saison sont exclusives : un produit `SEASONAL` n'est pas vendable le jour du début de sa saison.
* Un produit `EXPIRABLE` sans date d'expiration fait échouer le traitement de la commande.
* Le type de produit est stocké sous forme de chaîne (`ProductType`) : une énumération avec conversion EF serait plus sûre.

### Pistes d'amélioration

* Séparer les couches en projets distincts (Domain / Application / Infrastructure / Api) pour que le compilateur
  garantisse le sens des dépendances.
* Monter en version .NET (le projet cible .NET 7, qui n'est plus supporté) et utiliser `TimeProvider` (.NET 8).
* Ajouter de la journalisation (`ILogger`) dans le service applicatif.
