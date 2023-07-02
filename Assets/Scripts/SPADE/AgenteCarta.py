import random
import sys

from spade.agent import Agent
from spade.message import Message
from spade.behaviour import *

import AgenteManager as manager
import Actions 

import numpy as np
from typing import Tuple

##############################
#                            #
#           CARTAS           #
#                            #
##############################


CARD_WAIT = "CARD_WAIT"
CARD_ACTION = "CARD_ACTION"
    
class CardAgent(Agent):
    nombre = " "
    team = " "
    hp = 0
    ac = 0
    dmg = 0
    dex = 0
    rango = 0
    mov = 0
    shield = 0
    prio = 0
    pos = Tuple
    
    async def setup(self):
        print("He iniciado mi SETUP")
        self.team = sys.argv[1]
        self.hp = int(sys.argv[2])
        self.ac = int(sys.argv[3])
        self.dmg = int(sys.argv[4])
        self.dex = int(sys.argv[5])
        self.rango = int(sys.argv[6])
        self.mov = int(sys.argv[7])
        self.pos = int(sys.argv[8])
        self.nombre = int(sys.argv[9])
        self.shield = 0
        self.prio = random.randint(1,20) + self.dex

        behav = CardBehav()
        self.add_behaviour(behav)

        msg = Message(to="pvidal_manager@lightwitch.org")
        msg.set_metadata("Action", "Create")
        msg.body = self
        
        await self.send(msg)

        # ESTADOS
        behav.add_state(name=CARD_WAIT, state=CardWait(), initial=True)
        behav.add_state(name=CARD_ACTION, state=CardAction())

        # TRANSITIONS
        behav.add_transition(source=CARD_WAIT, dest=CARD_ACTION)
        behav.add_transition(source=CARD_ACTION, dest=CARD_ACTION)
        behav.add_transition(source=CARD_ACTION, dest=CARD_WAIT)

class CardBehav(FSMBehaviour):
    async def on_start(self):
        print(f'{self.agent.name} created in pos: {self.agent.pos}\n')
        print(f'{self.agent.nombre}, {self.agent.team}, {self.agent.prio}, {self.agent.pos}')

    async def on_end(self):
        await self.agent.stop()
        
class CardWait(State):
    async def run(self):
        print("CARDINIT")
        self.agent.Allies = manager.AgenteManager.AllyAgents
        self.agent.Enemies = manager.AgenteManager.AxisAgents

        self.set_next_state(CARD_ACTION)

class CardAction(State):
    async def run(self):
        # enemy_names = list(AgentManager.AxisAgents.keys)
        # enemy_values = list(AgentManager.AxisAgents.values)
        # enemy_dist = []
        # min_dist = 0

        # for n in enemy_values:
            # enemy_dist.append(self.Dist(self.agent.pos, n))
            
        # min_dist = enemy_dist.index(min(enemy_dist))
        # enemy = enemy_names[min_dist]
         
        await self.Attack(3, self.agent)
        print(f'{self.agent.name} on {self.agent.pos}')

        self.set_next_state(CARD_WAIT)

if __name__ == "__main__":
    carta = CardAgent(f'{CardAgent.name}@lightwitch.org', 'Pepelxmpp11,')
    future = carta.start()
    future.result()

    while carta.is_alive():
        try:
            time.sleep(1)
        except KeyboardInterrupt:
            break
    carta.stop()
    
    