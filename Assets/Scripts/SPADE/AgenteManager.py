import sys
import socket as s
import threading as t
import spade
import json

from spade.agent import Agent
from spade.message import Message
from spade.behaviour import *

import Actions

import numpy as np
from typing import Tuple

##############################
#                            #
#          MANAGER           #
#                            #
##############################

PREPARE_DECKS = "PREPARE_DECKS"
GAME_START = "GAME_START"
PLAYER_PLAY_CARDS = "PLAYER_PLAY_CARDS"
ENEMY_PLAY_CARDS = "ENEMY_PLAY_CARDS"
CARD_ACTIONS = "CARD_ACTIONS"
GAME_OVER = "GAME_OVER"

class AgentManager(Agent):
    Agentes = []
    AllyAgents = []
    AxisAgents = []

    async def setup(self):
        # self.nombre = self.name
        # self.Table = sys.argv[1]
        print("Estoy en el SETUP")
        
        # CREAR SERVER DE SPADE
        self.spade_socket = s.socket(s.AF_INET, s.SOCK_STREAM)
        self.spade_socket.setsockopt(s.IPPROTO_TCP, s.TCP_NODELAY, True)
        self.spade_socket.bind(('localhost', 8001))
        self.spade_socket.listen(5)
        print("Escuchando en localhost, 8001")

        # CONECTARME A SERVER DE UNITY
        self.unity_socket = s.socket(s.AF_INET, s.SOCK_STREAM)
        self.unity_socket.setsockopt(s.IPPROTO_TCP, s.TCP_NODELAY, True)
        self.unity_socket.connect(('localhost', 8000))
        print("Conectado a localhost, 8000")

        # INICIALIZAR LAS ACCIONES
        self.actions = Actions.Actions(self.spade_socket, self.unity_socket)
        
        self.tcp_listener_thread = t.Thread(target=self.listen_for_messages)
        self.tcp_listener_thread.start()

        behav = ManagerBehav()

        # ESTADOS
        behav.add_state(name = PREPARE_DECKS, state = PrepareDecks(), initial = True)
        behav.add_state(name = GAME_START, state = GameStart())
        behav.add_state(name = PLAYER_PLAY_CARDS, state = PlayerPlayCards())
        behav.add_state(name = ENEMY_PLAY_CARDS, state = EnemyPlayCards())
        behav.add_state(name = CARD_ACTIONS, state = CardActions())
        behav.add_state(name = GAME_OVER, state = GameOver())

        # TRANSICIONES
        behav.add_transition(source = PREPARE_DECKS, dest = GAME_START)
        behav.add_transition(source = GAME_START, dest = GAME_START)
        behav.add_transition(source = GAME_START, dest = PLAYER_PLAY_CARDS)
        behav.add_transition(source = PLAYER_PLAY_CARDS, dest = ENEMY_PLAY_CARDS)
        behav.add_transition(source = ENEMY_PLAY_CARDS, dest = CARD_ACTIONS)
        behav.add_transition(source = CARD_ACTIONS, dest = PLAYER_PLAY_CARDS)
        behav.add_transition(source = CARD_ACTIONS, dest = GAME_OVER)

        self.add_behaviour(behav)

    def listen_for_messages(self):
        while True:
            try:
                client_socket, address = self.spade_socket.accept()
                message = client_socket.recv(1024).decode("utf-8")
                if message == "close":
                    print(message)

                client_socket.close()
            except Exception as e:
                print("Error listening for messages:", str(e))
                break
            finally:
                self.spade_socket.close()


class ManagerBehav(FSMBehaviour):        
    async def on_start(self):
        print("El behaviour ha empezado!")

    async def on_end(self):
        self.agent.spade_socket.close()
        self.agent.unity_socket.close()
        await self.agent.stop()

##############################
#                            #
#          ESTADOS           #
#                            #
##############################

class PrepareDecks(State):
    async def run(self):
        print("State: GAME_START")
        player_deck = self.agent.actions.create_deck()
        player_deck = self.agent.actions.deck_to_json_action("create_player_deck", player_deck)
        await self.agent.actions.send_action_to_socket(player_deck)

        enemy_deck = self.agent.actions.create_deck()
        enemy_deck = self.agent.actions.deck_to_json_action("create_enemy_deck", enemy_deck)
        await self.agent.actions.send_action_to_socket(enemy_deck)

        self.set_next_state(GAME_START)

class GameStart(State):
    async def run(self):
        if await self.agent.actions.recv_message_from_socket(self.agent.spade_socket) != "Start":
            self.set_next_state(GAME_START)
        else:
            self.set_next_state(PLAYER_PLAY_CARDS)

class PlayerPlayCards(State):
    async def run(self):
        print("Player 1 PLAY YOUR CARDS")
        agent = await self.receive(timeout=30)
        if agent:
            print(f'agent: {agent.name}')
            self.agent.Agentes.append(agent)
            self.agent.AllyAgents.append(agent)
            self.agent.Agentes = Actions.Actions.order_cards_by_prio(self.agent.Agentes)
            # data = Actions.Actions.recv_message_from_socket(self.agent.spade_socket)
            
            self.next_state(ENEMY_PLAY_CARDS)
        else:
            print("NO HE ENTRADO EN EL IF")
            self.set_next_state(PLAYER_PLAY_CARDS)

class EnemyPlayCards(State):
    async def run(self):
        print("Player 2 PLAY YOUR CARDS")
        agent = await self.agent.spade_socket.recv(timeout=60)
        if agent:
            self.agent.Agentes.append(agent)
            self.agent.AllyAgents.append(agent)
            self.agent.Agentes = Actions.Actions.order_cards_by_prio(self.agent.Agentes)
            # data = Actions.Actions.recv_message_from_socket(self.agent.spade_socket)
            
            self.next_state(ENEMY_PLAY_CARDS)
        else:
            self.set_next_state(CARD_ACTIONS)

class CardActions(State):
    async def run(self):
        print("Let the cards do their things")
        data = Actions.Actions.recv_message_from_socket(self.agent.spade_socket)
        if data:
            self.next_state(CARD_ACTIONS)
        else:
            self.set_next_state(GAME_OVER)

class GameOver(State):
    async def run(self):
        print("THE GAME IS OVER")
        
##############################
#                            #
#            MAIN            #
#                            #
##############################

if __name__ == "__main__":
    manager = AgentManager("pvidal_manager@lightwitch.org", "Pepelxmpp11,")
    future = manager.start()
    future.result()

    while manager.is_alive():
        try:
            time.sleep(1)
        except KeyboardInterrupt:
            break
    manager.stop()