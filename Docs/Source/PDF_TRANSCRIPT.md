# Wickfall_Idee_Complete.pdf — transcript

The design source of truth, transcribed so it survives in text form. Section numbers are the PDF's.
The French is verbatim; each section is followed by an English translation. **If anything here
disagrees with the PDF, the PDF wins.**

---

## Page 1

### WICKFALL — *Jeu d'action-RPG 2D en vue de dessus, en pixel art*

### 1. Concept de l'idée

> Vous êtes le dernier **Porte-mèche**, un golem de bougie qui porte un éclat du Grand Foyer éteint.
> **Votre barre de vie est votre lumière.** Chaque coup reçu rétrécit ce que vous voyez. Chaque
> ennemi vaincu vous rend de la cire, mais plus vous brillez, plus les Creux (des vides en forme de
> choses autrefois éclairées) vous détectent de loin.
>
> **Règle d'or : on ne peut pas être fort et caché en même temps.**
>
> - **Une seule ressource :** la cire = vie = lumière = risque.
> - **Objectif :** traverser six régions pour rallumer le Grand Foyer.
> - **Public :** fans de Hollow Knight, Tunic, Death's Door. 16 ans et plus.
> - **Durée :** 12 à 15 heures de campagne, sessions de 25 à 40 minutes.
> - **Fins :** A) rallumer le Foyer et mourir, B) garder la flamme et rester seul, C) (secrète)
>   passer la mèche à un autre, avec les 6 reliques et le Roi Creux épargné.

*English.* A 2D top-down action-RPG in pixel art. You are the last Wickbearer, a candle golem carrying
a shard of the extinguished Great Hearth. Your health bar is your light: every hit you take shrinks
what you can see. Every enemy you defeat gives wax back, but the brighter you shine, the farther the
Hollows (voids shaped like things that were once lit) can detect you.
**Golden rule: you cannot be strong and hidden at the same time.**
One resource: wax = life = light = risk. Goal: cross six regions to relight the Great Hearth.
Audience: fans of Hollow Knight, Tunic, Death's Door, 16+. Length: a 12–15 hour campaign in 25–40
minute sessions. Endings: A) relight the Hearth and die, B) keep the flame and stay alone,
C) (secret) pass the wick on to another — with the 6 relics and the Hollow King spared.

### 2. Conception

#### 2.1 Le joueur

> - **Cire :** 100 au maximum. Perte passive de 0,5 par seconde : rester immobile, c'est perdre.
> - **Lumière :** rayon de 1,2 à 6 unités selon la cire restante.
> - **Actions :** déplacement 4 directions, attaque, esquive, changement de flamme, interaction.
> - **Tailler la mèche (maintenir une touche) :** rayon -65 %, perte -75 %, détection -60 %, dégâts
>   infligés -50 %, plus d'arts de flamme. C'est un mode « pauvreté », pas une invisibilité.
> - **Mort :** la flamme s'éteint (cire à 0), retour au dernier brasero.

| Flamme | Rôle | Coût en cire | Dégâts | Lumière |
|---|---|---|---|---|
| Ambre | Équilibrée, arc de mêlée | x1,0 | x1,0 | x1,0 |
| Bleue | À distance, ralentit | x0,9 | x0,7 | x0,85 |
| Verte | Poison, touche les groupes | x1,4 | x0,9 | x1,1 |
| Blanche | Anti-Creux, énorme lumière | x1,2 | x1,6 | x1,8 |
| Cramoisie | Plus de dégâts à faible cire | x1,6 | x0,6 à 2,2 | x1,3 |

> Changer de flamme coûte 8 de cire et provoque un éclat qui attire les ennemis.

*English.* Wax: 100 maximum; passive loss of 0.5 per second — standing still is losing. Light: radius
1.2 to 6 units depending on remaining wax. Actions: 4-direction movement, attack, dodge, flame switch,
interact. Trim the wick (hold a key): radius −65 %, loss −75 %, detection −60 %, damage dealt −50 %,
no flame arts — a "poverty" mode, not invisibility. Death: the flame goes out (wax at 0), back to the
last brazier. Flame table as above (Amber balanced melee arc; Blue ranged, slows; Green poison, hits
groups; White anti-Hollow, huge light; Crimson more damage at low wax). Switching flame costs 8 wax
and causes a flare that attracts enemies.

#### 2.2 Environnements et obstacles

| Région | Environnement | Obstacles | Ennemis |
|---|---|---|---|
| 1. Champs de Suif | Plaine de cire, lumière ambiante faible | Geysers de cire (10 dégâts, annoncés 1,2 s) | Driplings, Cirelles (inoffensives) |
| 2. Bois-Mèche | Forêt noire, canopée qui bloque la lumière, vignes à grimper | Fleurs mange-lumière (lumière coupée 3 s) | Carcasses (embuscade), Rôdeurs (bougent si vous brillez) |
| 3. Fonderies de Suie | Usine, convoyeurs, fours qui vacillent | Presses (35 dégâts), jets de vapeur, convoyeurs | Scorifiés (armurés), Soufflets (tirent de la suie, -10 cire) |

## Page 2

| Région | Environnement | Obstacles | Ennemis |
|---|---|---|---|
| 4. Cathédrale de Verre | Miroirs noirs, faisceaux physiques à orienter | Lasers (25 dégâts), sols de verre qui cassent sous la flamme cramoisie | Reflets (copient vos 3 dernières actions) |
| 5. Creux Profond | Aucune lumière ambiante, carte visible seulement où vous brillez | Perte de cire x1,5 | Les Éteints (dérivent vers la lumière la plus forte) |
| 6. Chambre du Foyer | Cathédrale de cendre, Foyer en lumière négative | Cendre qui ralentit | Roi Creux (boss en 3 phases) |

*English.* 1 Tallow Fields — wax plain, low ambient light; wax geysers (10 damage, telegraphed 1.2 s);
Driplings, Waxflies (harmless). 2 Wickwood — black forest, canopy that blocks light, vines to climb;
Light-Eater Blooms (light cut for 3 s); Husks (ambush), Lurkers (move if you shine). 3 Soot Foundries —
factory, conveyors, flickering furnaces; presses (35 damage), steam jets, conveyors; Slagborn
(armored), Bellows (shoot soot, −10 wax). 4 Glass Cathedral — black mirrors, physical beams to aim;
lasers (25 damage), glass floors that break under the crimson flame; Reflections (copy your last 3
actions). 5 Deep Hollow — no ambient light, map visible only where you shine; wax loss ×1.5; the Unlit
(drift toward the strongest light). 6 Hearth Chamber — ash cathedral, the Hearth in negative light;
ash that slows; the Hollow King (boss in 3 phases).

#### 2.3 Interactions entre les éléments

| Cause | Effet |
|---|---|
| Cire qui baisse | Rayon de lumière plus petit, vision réduite. |
| Rayon de lumière | Détection des ennemis = 2 + rayon x 1,5. Plus on brille, plus on est repéré. |
| Tuer un ennemi | Gain de 4 à 15 de cire, donc plus de lumière et plus de bruit. |
| Tailler la mèche | Discrétion et économie de cire, mais combat affaibli. |
| Suie (Soufflets) | Dégâts x1,25 et perte de lumière pendant 1,5 s. |
| Canopée, ombres, murs | Bloquent la lumière : poches d'obscurité où se cacher. |
| Flamme contre matière | Scorifiés blessés seulement par flamme blanche ou verte ; verre cassé par la cramoisie ; chaque panneau de la Rose demande une flamme différente. |
| Lumière contre ennemis | Rôdeurs bougent si vous brillez ; Éteints attirés par la source la plus forte ; Reflets copient vos actes. |
| Goutte de cire au sol | Laissée quand on est touché : sert de fil d'Ariane dans le Creux Profond. |

*English.* Lower wax → smaller light radius. Detection = 2 + radius × 1.5 (shine more, be spotted
sooner). A kill gives 4 to 15 wax (more light, more noise). Trimming: stealth and wax savings, weaker
combat. Soot: damage ×1.25 and light loss for 1.5 s. Canopy, shadows and walls block light: pockets of
darkness to hide in. Flame vs matter: Slagborn hurt only by white or green; glass broken by crimson;
each panel of the Rose needs a different flame. Light vs enemies: Lurkers move if you shine; the Unlit
are drawn to the strongest source; Reflections copy your actions. A wax drop is left on the ground when
you are hit; in the Deep Hollow it is an Ariadne's thread.

#### 2.4 Progression, score et récompenses

> - **Pas d'expérience.** On progresse avec 6 Reliques de Braise (une par région) : esquive en plus,
>   emplacement de flamme en plus, cire maximale, accroche au mur...
> - **Éclats de cire (monnaie) :** dépensés aux braseros pour des consommables et améliorer les flammes.
> - **Pactes du Roi Creux (optionnels) :** puissance contre un coût permanent (ex. +30 cire maximale,
>   perte x1,5).
> - **Brasero :** point de contrôle, remplit toute la cire, un usage par repos.
> - **Score de région (pour rejouer) :** cire restante en % x 10, plus bonus de temps, plus 500 par
>   relique, plus 20 par ennemi évité sans combat, moins 150 par mort. Rangs : Cendre, Braise, Flamme,
>   Brasier. Valeurs à ajuster en test.

*English.* No experience points: progress through 6 Ember Relics (one per region): an extra dodge, an
extra flame slot, more maximum wax, wall grip… Wax shards (currency) are spent at braziers on
consumables and flame upgrades. Hollow King's Pacts (optional): power for a permanent cost (e.g. +30
max wax, loss ×1.5). Brazier: checkpoint, refills all wax, one use per rest. Region score (for
replays): wax remaining in % × 10 + time bonus + 500 per relic + 20 per enemy avoided without fighting
− 150 per death. Ranks: Ash, Ember, Flame, Blaze. Values to be tuned in testing.

#### 2.5 Boucle de jeu

> Explorer en lumière faible, trouver une flamme de cire, un ennemi ou une relique, décider de
> briller ou non, combattre, récolter la cire, subir les conséquences (essaim, boss réveillé), puis se
> replier ou foncer jusqu'au brasero suivant.

*English.* Explore in low light; find a wax flame, an enemy or a relic; decide whether to shine;
fight; collect the wax; suffer the consequences (a swarm, a boss woken); then fall back or rush to the
next brazier.

### 3. Design : prompts pixel art pour Gemini

> Conseil : générer un composant par demande, demander une planche alignée sur une grille, puis
> nettoyer dans Aseprite. Les prompts sont en anglais pour de meilleurs résultats. Coller d'abord le
> style commun, puis le prompt du composant.

**STYLE COMMUN (à coller avant chaque prompt)**
```
16-bit pixel art, 2D top-down game asset, limited palette of charcoal black, bone white and desaturated ochre, only the light source is saturated and warm, clean 1-pixel outlines, no anti-aliasing, no text, no watermark, flat plain background, sprite sheet laid out on an even grid.
```

## Page 3

| Composant | Prompt (verbatim) |
|---|---|
| Joueur : marche | Small candle-golem character, 32x40 px, body of pale cream wax, a small glowing amber flame on its head, melancholic and determined look. Sprite sheet: 4 directions (down, up, left, right), walk cycle of 6 frames each. |
| Joueur : état faible | Same candle-golem in a dim state: tiny tight flame, hunched posture, wax melting and dripping. Idle animation, 4 frames, same size and palette. |
| Joueur : actions | Same candle-golem. Sprite sheet rows: sword-like fire arc attack (5 frames), dash with squash-and-stretch and wax trail (3 frames), hurt (2 frames), extinguished death collapsing into a wax puddle (12 frames). |
| Les 5 flammes (icônes 16x16) | Five flame icons with five different silhouettes so they are readable without color: amber round flame, blue teardrop flame, green leaf-shaped flame, white star-shaped flame, crimson jagged flame. Each on its own cell, glowing core, dark outline. |
| Ennemis des Champs de Suif | Dripling: slow 24x24 melee creature, a hunched blob of dripping pale wax with two dark holes for eyes. Waxfly: tiny 8x8 harmless glowing insect. Sprite sheet with 4-frame idle and 4-frame move each. |
| Ennemis du Bois-Mèche | Husk: 32x32 hollow bark-like creature hanging from branches, ambush pose. Lurker: 24x24 low crouching shadow creature, frozen pose and slow crawl. Light-Eater Bloom: 32x32 dark flower with a pale glowing core, 4-frame pulse. |
| Ennemis des Fonderies | Slagborn: 48x48 heavy armored golem of rusted metal and slag, slow walk, 6 frames. Bellows: 32x32 ranged machine with an accordion body that blows black soot projectiles, attack in 6 frames. Soot projectile: 8x8 black cloud, 4 frames. |
| Ennemis de la Cathédrale et du Creux Profond | Reflection: pure mirror-black copy of the player, same 32x40 size, pale edge highlights, glitchy 4-frame idle. The Unlit: shapeless pure-black silhouette 32x32 with a faint white outline, drifting animation, 6 frames. |
| Boss : le Roi Creux | Boss 128x160 px, a tall crowned figure made of pure black negative space, with a flame-shaped hole in its chest. Sprite sheet: idle, slam attack, and three phase variants (increasingly broken crown), 6 frames each. |
| Tilesets (16x16, répétables sans coupure) | One tileset per region, seamless 16x16 tiles: (1) cream hardened wax ground with 2-frame swaying wax grass and a wax geyser; (2) dark soil with roots and climbable wick-vines; (3) rusted metal grating with 4-frame animated conveyor and steam vent; (4) mirror-black glass floor with crystal edge tiles; (5) pure black ground with only faint tile outlines; (6) grey ash ground with slow-down drifts. Include wall tiles and corner tiles for each. |
| Objets et décors | Brazier (unlit and lit, 4-frame fire), wax pool (full and empty), Ember Relic (6 different relics, glowing), Great Bell (hanging, ringing 4 frames), the Broken Wick (huge candle stump landmark, 64x160), crystal pedestal for rotating light beams, locked gate door (closed and open). |
| Interface | HUD: wax bar drawn as a candle that melts down, 8 fill states; radial flame wheel with five slots; wax shard currency icon 16x16; small dash-charge icons; pause menu frame in bone white on charcoal. |
| Effets visuels | VFX sheets, 8 frames each: ember trail, wax drip hitting the ground, extinguish burst (radial), flare burst upward on flame swap, mirror laser beam segment, soot hit splash. |

*English (section 3 advice).* Generate one component per request, ask for a sheet aligned on a grid,
then clean up in Aseprite. Prompts are in English for better results. Paste the common style first,
then the component prompt.

## Page 4

### 4. Développement : les scripts et leur principe

> Moteur conseillé : Unity 6 avec rendu 2D URP (lumières 2D et ombres). Règle générale : tout le
> réglage est dans des ScriptableObjects, les systèmes se parlent par des canaux d'événements, et
> chaque entité a une machine à états.

| Script | Principe |
|---|---|
| WaxSystem | Seul propriétaire de la cire. Gère perte passive, dégâts, gains, mort. Tout le reste lit ou écoute cette valeur. |
| LightController | Transforme la cire en rayon, intensité et couleur de la lumière 2D, avec un léger scintillement. |
| DimController | Active ou coupe le mode « tailler la mèche » et applique ses multiplicateurs. |
| PlayerController et états | Machine à états : repos, marche, mode faible, attaque, esquive, blessé, éteint, interaction. |
| FlameSwitcher et FlameTypeSO | Choix de la flamme, coût en cire, éclat qui attire. Chaque flamme est une donnée réglable. |
| Hitbox et Damageable | Les attaques activent une zone de dégâts, la cible applique les multiplicateurs selon le type. |
| EnemyPerception | Calcule le rayon de détection depuis la lumière du joueur et garde la dernière position connue. |
| EnemyBrain | Machine à états des ennemis : errance, alerte, poursuite, attaque, retour. Chaque type ajoute son comportement. |
| BossHollowKing | Trois phases qui retirent chacune un appui (pas de mode faible, puis cire maximale à 40). |
| WaxPickup et WaxPool | Récompense de cire à la mort d'un ennemi et bassins de recharge dans le monde. |
| Brazier | Point de contrôle : sauvegarde, remplit la cire, boutique d'éclats. |
| LightPuzzle | Faisceaux orientables par piédestaux, panneaux demandant un type de flamme précis. |
| Shader LightReveal | Dans le Creux Profond, n'affiche le sol que là où la lumière du joueur arrive. |
| RoomLoader et SceneLoader | Chargement additif par groupe de salles, déchargement à distance. |
| ScoreSystem | Calcule le score de région depuis cire, temps, reliques, ennemis évités et morts. |
| SaveSystem | Sauvegarde chiffrée de la position, de la cire, des reliques, des pactes et des fins débloquées. |
| EventChannels | Messages entre systèmes sans références directes (cire changée, joueur éteint, boss vaincu). |
| HUD et FlameWheel | Barre de cire en bougie, roue de flammes, éclats, écoutent les canaux d'événements. |
| AudioReactive | Le crépitement de la flamme monte en hauteur quand la cire baisse ; battement de coeur dans le Creux Profond. |
| Pooling et Debug | Réutilisation des projectiles et effets (zéro allocation), outils de réglage de la détection. |

*English.* Recommended engine: Unity 6 with the 2D URP renderer (2D lights and shadows). General
rule: all tuning lives in ScriptableObjects, systems talk through event channels, and every entity
has a state machine. Scripts: WaxSystem (sole owner of wax: passive loss, damage, gains, death;
everything else reads or listens); LightController (wax → radius, intensity and colour of the 2D light,
with a slight flicker); DimController (turns trim mode on/off and applies its multipliers);
PlayerController and states (idle, walk, dim mode, attack, dodge, hurt, extinguished, interact);
FlameSwitcher and FlameTypeSO (flame choice, wax cost, the attracting flare; each flame is tunable
data); Hitbox and Damageable (attacks switch on a damage zone; the target applies multipliers by
type); EnemyPerception (detection radius from the player's light; remembers the last known position);
EnemyBrain (enemy state machine: wander, alert, chase, attack, return; each type adds behaviour);
BossHollowKing (three phases, each removing one support: no dim mode, then max wax capped at 40);
WaxPickup and WaxPool (kill reward; refill pools); Brazier (checkpoint: saves, refills wax, shard
shop); LightPuzzle (beams aimed by pedestals; panels needing a specific flame); LightReveal shader (in
the Deep Hollow, shows the ground only where the player's light reaches); RoomLoader and SceneLoader
(additive loading by room group, unloading when far away); ScoreSystem (region score from wax, time,
relics, enemies avoided and deaths); SaveSystem (encrypted save of position, wax, relics, pacts and
unlocked endings); EventChannels (messages without direct references: wax changed, player
extinguished, boss defeated); HUD and FlameWheel (candle wax bar, flame wheel, shards; they listen to
event channels); AudioReactive (the flame crackle rises in pitch as wax drops; heartbeat in the Deep
Hollow); Pooling and Debug (reuse projectiles and effects — zero allocation; detection tuning tools).
