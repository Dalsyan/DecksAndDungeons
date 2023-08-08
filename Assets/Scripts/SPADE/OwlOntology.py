import random

from owlready2 import *

class OntologyActions:
    def __init__(self, onto = get_ontology("D:\TEMP\dungeons-and-dragons.owx").load()):
        self.onto = onto

    ##############################
    #                            #
    #           ACTIONS          #
    #                            #
    ##############################

    # USER MANAGEMENT
    def create_user(self, name, password):
        users = self.onto.search(type = self.onto.CUser)

        for user in users:
            if name == user.name:
                print("este usuario ya existe")
                return

        cuser = self.onto.search(iri = "*CUser")[0]
        user = cuser()

        user.name = name
        user.password = password

        self.onto.save(file = "D:\TEMP\dungeons-and-dragons.owx", format = "rdfxml")
        return user

    def verify_user_login(self, user_name, password):
        user = self.onto.search(iri=f"*{user_name}").first()

        if user and user.password == password:
            return user

        return None
    
    def add_deck_to_user(self, user, deck):
        user.hasDecks.append(deck)

    # SEARCHING IN ONTOLOGY
    def search_for_decks(self, player : str):
        cplayer = self.onto.search(iri = f"*{player}")[0]
        decks = cplayer.hasDecks
        
        decks = []
        
        for deck in cplayer.hasDecks:
            decks.append(deck.name)
            
        return decks
    
    def search_for_deck(self, name):
        deck = self.onto.search(iri = f"*{name}")[0]
        return deck

    def search_for_cards(self, player):
        player = self.onto.search(iri = f"*{player}")[0]
        decks = player.hasDecks

        cards_list = []

        for deck in decks:
            for card in deck.hasCards:
                cards_list.append(self.card_to_dict_redux(card))

        return cards_list

    def search_for_deck_cards(self, deck):
        deck = self.onto.search(iri = f"*{deck}")[0]
        cards = deck.hasCards

        cards_list = []

        for card in cards:
            cards_list.append(self.card_to_dict(card))

        return cards_list
    
    def search_for_card(self, name):
        card = self.onto.search(iri = f"*{name}")[0]
        return card

    # PARSER
    def deck_to_list(self, cdeck):
        deck_json = []

        for ccard in cdeck.hasCards:
            card_json = self.card_to_dict(ccard)
            deck_json.append(card_json)
            
        return deck_json

    def deck_to_list_redux(self, cdeck):
        deck_json = []

        for ccard in cdeck.hasCards:
            card_json = self.card_to_dict(ccard)
            deck_json.append(card_json)
            
        return deck_json

    def card_to_dict(self, ccard):
        stats = {}

        print(ccard.name)

        creature_list = self.onto.search(type = self.onto.CCreature)
        print(creature_list)
        artifact_list = self.onto.search(type = self.onto.CArtifact)
        print(artifact_list)
        spell_list = self.onto.search(type = self.onto.CSpell)
        print(spell_list)
        
        if ccard in creature_list:
            stats["name"] = ccard.name
            cclass = ccard.hasClass
            stats["class"] = ''.join(filter(str.isalpha, cclass.name))
            crace = ccard.hasRace
            stats["race"] = ''.join(filter(str.isalpha, crace.name))
        
            stats["type"] = "creature"
            #stats["role"] = ccard.role
            stats["level"] = ccard.level
            stats["hp"] = ccard.hp
            stats["ac"] = ccard.ac
            #stats["str"] = ccard.str
            #stats["con"] = ccard.con
            #stats["dex"] = ccard.dex
            stats["damage"] = ccard.damage
            stats["magic"] = ccard.magic
            stats["range"] = ccard.range

        elif ccard in artifact_list:
            stats["type"] = "artifact"
            stats["name"] = ccard.name
            stats["power"] = ccard.power

        elif ccard in spell_list:
            stats["type"] = "spell"
            stats["name"] = ccard.name
            stats["power"] = ccard.power
            
        return stats
    
    def card_to_dict_redux(self, ccard):
        stats = {}

        stats["name"] = ccard.name
        
        stats["level"] = ccard.level
        stats["hp"] = ccard.hp
        stats["ac"] = ccard.ac
        stats["damage"] = ccard.damage
            
        return stats

    # CREATION
    def create_deck(self):
        num_cards = 0
        cdeck = self.onto.search(iri = "*CDeck")[0]
        my_deck = cdeck()

        while num_cards != 5:
            card = self.create_card(random.randint(1,3))
            my_deck.hasCards.append(card)
            num_cards += 1
            
        self.onto.save(file = "D:\TEMP\dungeons-and-dragons.owx", format = "rdfxml")
        return my_deck
    
    def create_player_deck(self, player):
        num_cards = 0
        cdeck = self.onto.search(iri = "*CDeck")[0]
        cplayer = self.onto.search(iri = f"*{player}")[0]
        my_deck = cdeck()

        while num_cards != 5:
            card = self.create_card(random.randint(1,3))
            my_deck.hasCards.append(card)
            num_cards += 1

        while num_cards != 10:
            card = random.choice([self.create_artifact(), self.create_spell()])
            my_deck.hasCards.append(card)
            num_cards += 1

        cplayer.hasDecks.append(my_deck)

        self.onto.save(file = "D:\TEMP\dungeons-and-dragons.owx", format = "rdfxml")
        return my_deck

    def remove_deck(self, cdeck):
        for ccard in cdeck.hasCards:
            self.remove_card(ccard)
            
        print(f'I have removed the deck {cdeck.name}')
        destroy_entity(cdeck)

        self.onto.save(file = "D:\TEMP\dungeons-and-dragons.owx", format = "rdfxml")

    def create_card(self, level):
        cclass = self.create_class()
        crace = self.create_race()
        cweapon = self.create_weapon()
        carmor = self.create_armor(cweapon)

        ccard = self.onto.search(iri = "*CCreature")[0]
        my_card = ccard()

        my_card.hasClass = cclass
        my_card.hasRace = crace
        my_card.hasWeapon = cweapon
        
        my_card.level = level
        my_card.role = my_card.hasClass.role
        my_card.hp = (my_card.level * cclass.hp) + self.skill_mods(crace.con)
        my_card.str = crace.str
        my_card.con = crace.con
        my_card.dex = crace.dex
        my_card.damage = cweapon.damage
        my_card.magic = crace.magic
        my_card.range = cweapon.range
        my_card.mov = 3

        if len(carmor) == 1:
            my_card.hasArmor = carmor[0]
            if carmor[0].ac == 0:
                my_card.ac = 10 + self.skill_mods(crace.dex)
            elif carmor[0].ac >= 16:
                my_card.ac = carmor[0].ac
            else:
                my_card.ac = carmor[0].ac + self.skill_mods(crace.dex)
        else:
            my_card.hasArmor = carmor[0] # Armor
            my_card.hasShield = carmor[1] # Shield
            if carmor[0].ac == 0:
                my_card.ac = 10 + self.skill_mods(crace.dex) + carmor[1].ac
            if carmor[0].ac >= 16:
                my_card.ac = carmor[0].ac + carmor[1].ac
            else:
                my_card.ac = max(carmor[0].ac + carmor[1].ac + self.skill_mods(crace.dex), 20)
                
        self.onto.save(file = "D:\TEMP\dungeons-and-dragons.owx", format = "rdfxml")

        return my_card
    
    def remove_card(self, ccard):
        cclass = ccard.hasClass
        destroy_entity(cclass)
        crace = ccard.hasRace
        destroy_entity(crace)
        cweapon = ccard.hasWeapon
        destroy_entity(cweapon)
        carmor = ccard.hasArmor
        destroy_entity(carmor)

        if ccard.hasShield:
            cshield = ccard.hasShield
            destroy_entity(cshield)

        print(f'I have removed the card {ccard.name}')
        destroy_entity(ccard)

        self.onto.save(file = "D:\TEMP\dungeons-and-dragons.owx", format = "rdfxml")

    def create_spell(self):
        ccard = self.onto.search(iri = "*CSpell")[0]
        my_card = ccard()
        
        spell_type = random.choice(["Evocation","Healing"]) 
        ctype = self.onto.search(iri = f"*{spell_type}")[0]
        
        my_card.knowsSpell = ctype
        my_card.power = random.randint(1,3)
        
        self.onto.save(file = "D:\TEMP\dungeons-and-dragons.owx", format = "rdfxml")

        return my_card

    def create_artifact(self):
        ccard = self.onto.search(iri = "*CArtifact")[0]
        my_card = ccard()
        
        artifact_type = random.choice(["Belt","Boots","Collar","Gloves"]) 
        ctype = self.onto.search(iri = f"*{artifact_type}")[0]

        my_card.hasItem = ctype
        my_card.power = random.randint(1,3)
        
        self.onto.save(file = "D:\TEMP\dungeons-and-dragons.owx", format = "rdfxml")

        return my_card

    ##############################
    #                            #
    #        ATTR_CREATION       #
    #                            #
    ##############################

    def create_class(self):
        class_list = self.onto.search(subclass_of = self.onto.CClass)
        class_list = class_list[4:]

        cclass = random.choice(class_list)
        my_class = cclass()
        my_class.hp = random.choice([6,8,10,12])

        dps_list = self.onto.search(subclass_of = self.onto.CDps)
        dps_list.pop(0)

        tank_list = self.onto.search(subclass_of = self.onto.CTank)
        tank_list.pop(0)

        mage_list = self.onto.search(subclass_of = self.onto.CMage)
        mage_list.pop(0)
        
        if cclass in dps_list:
            role = "dps"

        elif cclass in tank_list:
            role = "tank"

        elif cclass in mage_list:
            role = "mage"
            
        my_class.role = role

        self.onto.save(file = "D:\TEMP\dungeons-and-dragons.owx", format = "rdfxml")
        
        return my_class

    def create_race(self): 
        # Pick a random race
        race_list = self.onto.search(subclass_of = self.onto.Race)
        race_list.pop(0)
        crace = random.choice(race_list)
        my_race = crace()
        my_race.str = self.skill_dices()
        my_race.dex = self.skill_dices()
        my_race.con = self.skill_dices()
        my_race.magic = self.skill_dices()
        
        self.onto.save(file = "D:\TEMP\dungeons-and-dragons.owx", format = "rdfxml")
        
        return my_race

    def create_weapon(self):
        my_weapon = self.all_weapons()
        my_weapon.damage = random.choice([4,6,8,10,12])

        if my_weapon in self.ranged_weapon()[2]:
            my_weapon.range = 3
        else:
            my_weapon.range = 1

        self.onto.save(file = "D:\TEMP\dungeons-and-dragons.owx", format = "rdfxml")
        
        return my_weapon

    def create_armor(self, cweapon):
        carmor_list = []
        
        carmor = self.onto.search(subclass_of = self.onto.Armor)[0]
        cshield = self.onto.search_one(iri = "*Shield")
        
        my_armor = carmor()
        my_armor.ac = random.choice([0, 11, 12, 13, 14, 16, 18])
        carmor_list.append(my_armor)

        # RANGED WEAPON = NO SHIELD
        if cweapon in self.ranged_weapon()[2]:
            self.onto.save(file = "D:\TEMP\dungeons-and-dragons.owx", format = "rdfxml")
            
            return carmor_list

        if random.randint(1,2) == 1:
            my_shield = cshield()
            my_shield.ac = 2
            carmor_list.append(my_shield)
            self.onto.save(file = "D:\TEMP\dungeons-and-dragons.owx", format = "rdfxml")
            
            return carmor_list
            
        self.onto.save(file = "D:\TEMP\dungeons-and-dragons.owx", format = "rdfxml")
        
        return carmor_list

    def melee_weapon(self):
        weapons = []

        # SIMPLE
        melee_simple_list = self.onto.search(iri = "*MeleeSimple")
        weapons.append(melee_martial_list)

        # MARTIAL
        melee_martial_list = self.onto.search(iri = "*MeleeMartial")
        weapons.append(melee_martial_list)

        # ALL
        melee_weapon_list = melee_simple_list + melee_martial_list
        weapons.append(melee_weapon_list)
        return weapons

    def ranged_weapon(self):
        weapons = []

        # SIMPLE
        ranged_simple_list = self.onto.search(iri = "*RangedSimple")
        weapons.append(ranged_simple_list)

        # MARTIAL
        ranged_martial_list = self.onto.search(iri = "*RangedMartial")
        weapons.append(ranged_martial_list)

        # ALL
        ranged_weapon_list = ranged_simple_list + ranged_martial_list
        weapons.append(ranged_weapon_list)
        return weapons

    def all_weapons(self):
        weapons = []
        melee_simple_list = self.onto.search_one(iri = "*MeleeSimple")
        weapons.append(melee_simple_list)
        melee_martial_list = self.onto.search_one(iri = "*MeleeMartial")
        weapons.append(melee_martial_list)
        
        ranged_simple_list = self.onto.search_one(iri = "*RangedSimple")
        weapons.append(ranged_simple_list)
        ranged_martial_list = self.onto.search_one(iri = "*RangedMartial")
        weapons.append(ranged_martial_list)

        cweapon = random.choice(weapons)
        my_weapon = cweapon()
        return my_weapon

    ##############################
    #                            #
    #            DICES           #
    #                            #
    ##############################

    def skill_dices(self):
        dices = []
        while len(dices) < 4:
            dices.append(random.randint(1,6))
        dices.remove(min(dices))
        return sum(dices)

    def skill_mods(self, skill):
        if skill == 1:
            return -5
        elif 2 <= skill <= 3:
            return -4
        elif 4 <= skill <= 5:
            return -3
        elif 6 <= skill <= 7:
            return -2
        elif 8 <= skill <= 9:
            return -1
        elif 10 <= skill <= 11:
            return 0
        elif 12 <= skill <= 13:
            return 1
        elif 14 <= skill <= 15:
            return 2
        elif 16 <= skill <= 17:
            return 3
        elif 18 <= skill <= 19:
            return 4
        elif skill == 20:
            return 5