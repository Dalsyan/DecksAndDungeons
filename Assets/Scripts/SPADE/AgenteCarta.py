import random
import sys

from owlready2 import *
from spade.agent import Agent
from spade.message import Message
from spade.template import Template
from spade.behaviour import *

import AgenteManager
import Actions

import numpy as np
from typing import Tuple

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
    def __init__(self, jid, password, card, player_card_agents, enemy_card_agents):
        super().__init__(jid, password)
        self.cclass = card.cclass
        self.race = card.race
        self.owner = card.owner
        self.level = card.level
        self.hp = card.hp
        self.ac = card.ac
        self.str = card.strength
        self.con = card.con
        self.dex = card.dex
        self.damage = card.damage
        self.magic = card.magic
        self.range = card.rango
        self.prio = card.prio
        self.pos = card.pos
        self.shielded = False
        self.current_hp = self.hp

        if self.owner == "player":
            self.player_card_agents = player_card_agents
            self.enemy_card_agents = enemy_card_agents
        else:
            self.player_card_agents = enemy_card_agents
            self.enemy_card_agents = player_card_agents

        self.card_agents = self.player_card_agents + self.enemy_card_agents

    async def setup(self):
        print(f"Soy {self.agent.name} y he iniciado mi SETUP")

        # UTILES
        self.mov = 3
        self.attacks = 0
        self.minions = 0

        # INICIALIZAR LAS ACCIONES
        self.actions = Actions.Actions(self.spade_socket, self.unity_socket)

        # LO CARGAMOS EN TODOS LOS AGENTES ACTIVOS
        for agent in self.card_agents:
            if self.owner == "player":
                agent.player_card_agents.append(self)

            elif self.owner == "enemy":
                agent.enemy_card_agents.append(self)

            agent.card_agents.append(self)

        behav = CardBehav()

        # ESTADOS
        behav.add_state(name=CARD_WAIT, state=CardWait(), initial=True)
        behav.add_state(name=CARD_ACTION, state=CardAction())
        behav.add_state(name=CARD_STOP, state=CardStop())

        # TRANSICIONES
        behav.add_transition(source=CARD_WAIT, dest=CARD_ACTION)
        behav.add_transition(source=CARD_ACTION, dest=CARD_STOP)
        behav.add_transition(source=CARD_STOP, dest=CARD_WAIT)
        
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
        await self.agent.stop()

##############################
#                            #
#           STATES           #
#                            #
##############################
        
class CardWait(State):
    async def run(self):
        print("State: CARD_WAIT")

        message = await self.agent.receive()

        while not message:
            pass

        if message == "start_actions":
            print("State TO: CARD_ACTION")
            self.next_transition(CARD_ACTION)

class CardAction(State):
    async def run(self):
        print("State: CARD_ACTION")
        nearest_enemy = self.agent.actions.nearest_enemy(self.agent, self.agent.enemy_card_agents)
        
        if self.agent.actions.get_card_role(self.agent.card) == "cdps":
            if self.agent.attacks == 3:
                await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.damage, special = True)
                self.agent.attacks = 0

            else: 
                await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.damage)
                self.agent.attacks += 1

        elif self.agent.actions.get_card_role(self.agent.card) == "ctank":
            if self.agent.card.cclass == "paladin":
                player_card_agents_low = [card for card in self.agents.card_agents if ((card.current_hp * 100) / card.hp) < 34]
                lowest_player_card = player_card_agents_low.sort(Key = lambda x : x.current_hp)[0]

                if lowest_player_card:
                    await self.agent.actions.shield(self.agent.card, lowest_player_card)

                else: 
                    await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.damage)

            elif self.agent.card.cclass == "barbarian": 
                if ((self.agent.card.current_hp * 100) / self.agent.card.current_hp) <= 50:
                    await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.damage, special = True)

                else: 
                    await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.damage)

        elif self.agent.actions.get_card_role(self.agent.card) == "cmage":

            if self.agent.card.cclass == "cleric":
                player_card_agents_low = [card for card in self.agents.card_agents if ((card.current_hp * 100) / card.hp) < 34]
                lowest_player_card = player_card_agents_low.sort(Key = lambda x : x.current_hp)[0]

                if lowest_player_card:
                    await self.agent.actions.heal(self.agent.card, lowest_player_card)

                else: 
                    await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.magic)

            if self.agent.card.cclass == "druid":
                if self.agens.minions < self.agent.card.level:
                    # invoke minion
                    self.minions += 1

                else: 
                    await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.magic)

            else:
                await self.agent.actions.attack(self.agent.card, nearest_enemy, self.card.magic)
        
        print("State TO: CARD_STOP")
        self.next_transition(CARD_STOP)

class CardStop(State):
    async def run(self):
        print("State: CARD_STOP")

        msg = Message(to="pvidal_manager@lightwitch.org")
        msg.body = f"{self.agent.card.name}_done"

        await self.send(msg)

        self.next_transition(CARD_WAIT)