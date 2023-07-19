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
    def __init__(self, jid, password, card, unity_socket):
        super().__init__(jid, password)

        self.card = card
        self.cclass = card.hasClass
        self.race = card.hasRace
        self.owner = card.owner
        self.level = card.level
        self.hp = card.hp
        self.ac = card.ac
        self.str = card.str
        self.con = card.con
        self.dex = card.dex
        self.damage = card.damage
        self.magic = card.magic
        self.range = card.range
        self.prio = card.dex
        self.pos = card.pos

        self.mov = card.mov
        self.max_mov = card.mov

        self.shielded = False
        self.current_hp = self.hp
        
        self.ally_card_agents = []
        self.enemy_card_agents = []

        self.table = {}

        self.unity_socket = unity_socket

    async def setup(self):
        print(f"Soy {self.card.name} y he iniciado mi SETUP")

        # UTILES
        self.mov = 3
        self.attacks = 0
        self.minions = 0

        # INICIALIZAR LAS ACCIONES
        self.actions = Actions.Actions(unity_socket = self.unity_socket)

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
        
        nearest_enemy = self.agent.actions.nearest_enemy(self.agent, self.agent.enemy_card_agents)
        nearest_ally = self.agent.actions.nearest_ally(self.agent, self.agent.ally_card_agents)

        new_pos = self.agent.actions.move_to_card(self.agent, nearest_enemy, self.agent.table)
        if new_pos is not None:
            self.agent.pos = new_pos
            await self.agent.actions.send_action_to_socket({"action" : "move_card", "data": self.agent.card.name, "pos": new_pos})

            move_msg = Message(to="pvidal_manager@lightwitch.org", body = "move_to")
            move_msg.set_metadata("card", self.agent.card.name)
            move_msg.set_metadata("pos", self.agent.pos)
            await self.send(move_msg)

        else:
            sent_msg = Message(to = f'pvidal_manager@lightwitch.org')
            sent_msg.body = "stop"
            await self.send(sent_msg)

            time.sleep(1)

            await self.agent.stop()





        #move = self.agent.actions.move(self.agent, self.agent.table, "(0, 0)")
        #if move_to_someone:
        #    await self.agent.actions.send_action_to_socket({"action" : "move_card", "data": self.agent.card.name, "pos": self.agent.pos})

        #    move_msg = Message(to="pvidal_manager@lightwitch.org", body = "move_to")
        #    move_msg.set_metadata("card", self.agent.card.name)
        #    move_msg.set_metadata("pos", self.agent.pos)
        #    await self.send(move_msg)
            
        #if self.agent.actions.get_card_role(self.agent.card) == "dps":
        #    if self.agent.attacks == 3:
        #        await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.damage, special = True)
        #        self.agent.attacks = 0

        #    else: 
        #        await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.damage)
        #        self.agent.attacks += 1

        #elif self.agent.actions.get_card_role(self.agent.card) == "tank":
        #    if self.agent.card.cclass == "paladin":
        #        ally_card_agents_low = [card for card in self.agents.card_agents if ((card.current_hp * 100) / card.hp) < 34]
        #        lowest_player_card = ally_card_agents_low.sort(Key = lambda x : x.current_hp)[0]

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
        #        ally_card_agents_low = [card for card in self.agents.card_agents if ((card.current_hp * 100) / card.hp) < 34]
        #        lowest_player_card = ally_card_agents_low.sort(Key = lambda x : x.current_hp)[0]

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