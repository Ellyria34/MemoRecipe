# Template mail — Invitation Vague 1 (P0-7 requirement)

> **Usage** : template à personnaliser puis envoyer individuellement à chaque beta testeur Vague 1 (30 destinataires prévus, phase Beta.1). Alpha.3 = solo (propriétaire seule), aucune invitation envoyée à ce stade. Template prêt pour Beta.1 (US-B1-15).

---

## Objet du mail

`Votre accès à MemoRecipe — beta test`

## Corps du mail (personnaliser les [PLACEHOLDERS] avant envoi)

```
Bonjour [PRENOM],

Merci d'avoir accepté de tester MemoRecipe, l'application qui vous aide à
scanner, organiser et retrouver vos recettes de cuisine.

Voici vos identifiants d'accès :

URL : https://app.memorecipe.com
Email : [EMAIL_UTILISATEUR]

Votre mot de passe temporaire vous sera communiqué séparément via un canal
sécurisé (messagerie chiffrée ou appel téléphonique) pour éviter toute
interception. Merci de vous connecter dans les 7 jours à venir.

À votre première connexion, nous vous recommandons de changer votre mot de
passe dès que la fonctionnalité de changement en libre-service sera disponible.

En cas de perte de mot de passe : contactez contact@memorecipe.com depuis
votre adresse email enregistrée ci-dessus. Nous vérifierons votre identité
avant de procéder à une réinitialisation manuelle.

Confidentialité : consultez notre politique de confidentialité sur
https://app.memorecipe.com/privacy pour comprendre comment vos données sont
traitées (hébergement UE, chiffrement, RGPD).

Bug ou question : contactez contact@memorecipe.com.

Bon test !

L'équipe MemoRecipe
```

---

## Notes internes (ne pas envoyer aux destinataires)

- **Génération mdp temporaire** : utiliser `openssl rand -base64 12 | tr -d '/+='` pour un mot de passe aléatoire ~12 caractères alphanumériques.

- **Communication du mdp au user — RGPD Art. 32 (sécurité)** : le mot de passe ne doit PAS figurer dans le mail lui-même en clair, car les mails ne sont pas chiffrés bout-en-bout et peuvent être interceptés / archivés / relayés. Communication séparée obligatoire :
  - **Recommandé** : messagerie chiffrée (Signal, WhatsApp E2E, ProtonMail).
  - **Acceptable** : appel téléphonique en verbal (le user note le mdp puis le change à la 1ère connexion).
  - **Interdit** : SMS non chiffré, mail en clair, chat non chiffré.

- **Perte de mdp beta testeur** : suivre la procédure `documentation/DEPLOYMENT.md` section "Runbook incidents > User password reset (P0-7)".

- **Compte inactif** : si un beta testeur ne se connecte pas dans les 7 jours, renvoyer un rappel. Après 14 jours sans connexion, considérer supprimer le compte (RGPD Art. 5 minimisation).

- **US liée** : ce template est référencé dans US-B1-15 (invitations Vague 1 Beta.1) et livré en scope P0-7 (#81) car le body demande cette communication comme partie du runbook de reset mdp.
