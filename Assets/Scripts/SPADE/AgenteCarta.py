from owlready2 import *
from spade.agent import Agent
from spade.message import Message
from spade.behaviour import *

import Actions


##############################
#                            #
#          CONSTANTS         #
#                            #
##############################

CARD_WAIT = "CARD_WAIT"
CARD_ACTION = "CARD_ACTION"
CARD_STOP = "CARD_STOP"

##############################
#                            #
#            CARD            #
#                            #
##############################

class CardAgent(Agent):
    def __init__(self, jid, password, card):
        super().__init__(jid, password)

        self.card = card
        self.cclass = card.hasClass
        self.race = card.hasRace
        #self.owner = card.owner
        self.level = card.level
        self.hp = card.hp
        self.ac = card.ac
        self.str = card.str
        self.con = card.con
        self.dex = card.dex
        self.damage = card.damage
        self.magic = card.magic
        self.range = card.range
        #self.prio = card.prio
        #self.pos = card.pos
        self.shielded = False
        self.current_hp = self.hp
        
        self.player_card_agents = []
        self.enemy_card_agents = []

    async def setup(self):
        print(f"Soy {self.card.name} y he iniciado mi SETUP")

        # UTILES
        self.mov = 3
        self.attacks = 0
        self.minions = 0

        # INICIALIZAR LAS ACCIONES
        self.actions = Actions.Actions()

        behav = CardBehav()

        # ESTADOS
        behav.add_state(name=CARD_WAIT, state=CardWait(), initial=True)
        behav.add_state(name=CARD_ACTION, state=CardAction())
        behav.add_state(name=CARD_STOP, state=CardStop())

        # TRANSICIONES
        behav.add_transition(source=CARD_WAIT, dest=CARD_ACTION)
        behav.add_transition(source=CARD_ACTION, dest=CARD_STOP)
        #behav.add_transition(source=CARD_STOP, dest=CARD_WAIT)
        
        self.add_behaviour(behav)

##############################
#                            #
#          BEHAVIOUR         #
#                            #
##############################

class CardBehav(FSMBehaviour):
    async def on_start(self):
        print(f"Soy {self.agent.name} y mi behaviour ha empezado!")

    async def on_end(self):
        print(f"Soy {self.agent.name} y mi behaviour ha acabado!")
        await self.agent.stop()

##############################
#                            #
#           STATES           #
#                            #
##############################
        
class CardWait(State):
    async def run(self):
        print("State: CARD_WAIT")

        msg = await self.receive(10)

        if msg:
            if msg.body == "start":
                print("State TO: CARD_ACTION")
                self.set_next_state(CARD_ACTION)

        else: 
            print("no he recibido nada")
            await self.agent.stop()

class CardAction(State):
    async def run(self):
        print("State: CARD_ACTION")
        #nearest_enemy = self.agent.actions.nearest_enemy(self.agent, self.agent.enemy_card_agents)
        #print(f"{self.agent.card.name} nearest_enemy: {nearest_enemy.name}")
        
        #if self.agent.actions.get_card_role(self.agent.card) == "dps":
        #    if self.agent.attacks == 3:
        #        await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.damage, special = True)
        #        self.agent.attacks = 0

        #    else: 
        #        await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.damage)
        #        self.agent.attacks += 1

        #elif self.agent.actions.get_card_role(self.agent.card) == "tank":
        #    if self.agent.card.cclass == "paladin":
        #        player_card_agents_low = [card for card in self.agents.card_agents if ((card.current_hp * 100) / card.hp) < 34]
        #        lowest_player_card = player_card_agents_low.sort(Key = lambda x : x.current_hp)[0]

        #        if lowest_player_card:
        #            await self.agent.actions.shield(self.agent.card, lowest_player_card)

        #        else: 
        #            await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.damage)

        #    elif self.agent.card.cclass == "barbarian": 
        #        if ((self.agent.card.current_hp * 100) / self.agent.card.current_hp) <= 50:
        #            await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.damage, special = True)

        #        else: 
        #            await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.damage)

        #elif self.agent.actions.get_card_role(self.agent.card) == "mage":

        #    if self.agent.card.cclass == "cleric":
        #        player_card_agents_low = [card for card in self.agents.card_agents if ((card.current_hp * 100) / card.hp) < 34]
        #        lowest_player_card = player_card_agents_low.sort(Key = lambda x : x.current_hp)[0]

        #        if lowest_player_card:
        #            await self.agent.actions.heal(self.agent.card, lowest_player_card)

        #        else: 
        #            await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.magic)

        #    if self.agent.card.cclass == "druid":
        #        if self.agens.minions < self.agent.card.level:
        #            # invoke minion
        #            self.minions += 1

        #        else: 
        #            await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.magic)

        #    else:
        #        await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.magic)
        
        print("State TO: CARD_STOP")
        self.set_next_state(CARD_STOP)

class CardStop(State):
    async def run(self):
        print("State: CARD_STOP")

        sent_msg = Message(to = f'pvidal_manager@lightwitch.org')
        sent_msg.body = "stop"
        await self.send(sent_msg)

        time.sleep(1)

        await self.agent.stop()