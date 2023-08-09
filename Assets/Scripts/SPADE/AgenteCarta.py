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
        self.role = card.role

        self.mov = card.mov
        self.max_mov = card.mov

        self.shielded = False
        self.current_hp = self.hp
        
        self.card_agents = []
        self.ally_card_agents = []
        self.enemy_card_agents = []

        self.table = {}

        self.unity_socket = unity_socket

    async def setup(self):
        print(f"\nSoy {self.card.name} y he iniciado mi SETUP")

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
        print(f"Soy {self.agent.name} y mi behaviour ha acabado!\n")
        await self.agent.stop()

##############################
#                            #
#           STATES           #
#                            #
##############################
        
class CardWait(State):
    async def run(self):
        print("State: CARD_WAIT")
        print(f"soy {self.agent.cclass}")
        print(f"soy {''.join(filter(str.isalpha, self.agent.cclass.name))}")


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

        if self.agent.role == "dps":
            if self.agent.attacks == 3:
                await self.agent.actions.attack(self.agent, nearest_enemy, "ad", special = True)
                print("soy DPS y he hecho ataque ESPECIAL")
                self.agent.attacks = 0

            else: 
                await self.agent.actions.attack(self.agent, nearest_enemy, "ad")
                print("soy DPS y he hecho ataque NORMAL")
                self.agent.attacks += 1

        elif self.agent.role == "tank":
            if ''.join(filter(str.isalpha, self.agent.cclass.name)) == "paladin":
                ally_card_agents_low = [card for card in self.agent.card_agents if ((card.current_hp * 100) / card.hp) < 34]
                lowest_player_card = ally_card_agents_low.sort(Key = lambda x : x.current_hp)[0]

                if lowest_player_card is not None:
                    print("soy PALADIN e intento ESCUDAR")
                    await self.agent.actions.shield(self.agent.card, lowest_player_card)

                else: 
                    print("soy PALADIN y he hecho ataque NORMAL")
                    await self.agent.actions.attack(self.agent, nearest_enemy, "ad")

            elif ''.join(filter(str.isalpha, self.agent.cclass.name)) == "barbarian":
                print("soy barbarian")
                if ((self.agent.current_hp * 100) / self.agent.hp) <= 50:
                    print("soy BARBARO y he hecho ataque ESPECIAL")
                    await self.agent.actions.attack(self.agent, nearest_enemy, "ad", special = True)

                else: 
                    print("soy BARBARO y he hecho ataque NORMAL")
                    await self.agent.actions.attack(self.agent, nearest_enemy, "ad")

            else:
                print("soy tank pero me cago encima")

        elif self.agent.role == "mage":
            if ''.join(filter(str.isalpha, self.agent.cclass.name)) == "cleric":
                ally_card_agents_low = [card for card in self.agent.card_agents if ((card.current_hp * 100) / card.hp) < 34]
                lowest_player_card = ally_card_agents_low.sort(Key = lambda x : x.current_hp)[0]

                if lowest_player_card:
                    print("soy CLERIGO e intento CURAR")
                    await self.agent.actions.heal(self.agent.card, lowest_player_card)

                else: 
                    print("soy CLERIGO y he hecho ataque NORMAL")
                    await self.agent.actions.attack(self.agent, nearest_enemy, "ap")

            elif ''.join(filter(str.isalpha, self.agent.cclass.name)) == "druid":
                if self.agent.minions < self.agent.card.level:
                    print("soy DRUIDA e intento SPAWNEAR")
                    # invoke minion
                    self.agent.minions += 1

                else: 
                    print("soy DRUIDA y he hecho ataque NORMAL")
                    await self.agent.actions.attack(self.agent, nearest_enemy, "ap")

            else:
                print("soy MAGO/HECHICERO y he hecho ataque NORMAL")
                await self.agent.actions.attack(self.agent, nearest_enemy, "ap")
        
        print("State TO: CARD_STOP")
        self.set_next_state(CARD_STOP)

class CardStop(State):
    async def run(self):
        print("State: CARD_STOP")

        sent_msg = Message(to = f'pvidal_manager@lightwitch.org')
        sent_msg.body = "stop"
        await self.send(sent_msg)

        time.sleep(1)