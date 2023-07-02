import json
import random
from socket import socket as s

from spade.message import Message
from spade.behaviour import *

import AgenteCarta as card
import AgenteManager as manager
import OwlOntology

import numpy as np
from cmath import sqrt
from typing import Tuple

from owlready2 import *

class Actions:
    def __init__(self, spade_socket : s, unity_socket : s, owl = OwlOntology.OntologyActions):
        self.spade_sock = spade_socket
        self.unity_sock = unity_socket
        self.owl = owl()
        # self.onto = self.owl.onto

    ##############################
    #                            #
    #           CARTAS           #
    #                            #
    ##############################
    
    async def Dist(self, me : Tuple, other: Tuple) -> int:
        x1, y1 = me[0], me[1]
        x2, y2 = other[0], other[1]
        return sqrt((x2-x1)**2+(y2-y1)**2)

    async def CanMove(self, table : np.array, pos : Tuple) -> bool:
        if table[pos]:
            return False
        else: 
            return True

    async def Move(self, card : card, table : np.array, old_pos : Tuple, new_pos : Tuple):
        if self.CanMove(table, new_pos):
            table[old_pos] = False
            card.pos = new_pos
            table[new_pos] = True

    async def Attack(self, me : card, enemy : card, damage : int):
        if self.Dist(me.pos, enemy.pos) == 1:
            msg = Message(to=f'{enemy.name}@lightwitch.org')
            msg.set_metadata("Action", "Attack")
            msg.body = damage

            await self.send(msg)
            print("Message sent!")

    async def Shield(self, me : card, ally : card, shield : int):
        if self.Dist(me.pos, ally.pos) <= 2:
            msg = Message(to=f'{ally.name}@lightwitch.org')
            msg.set_metadata("Action", "Shield")
            msg.body = shield

            await self.send(msg)
            print("Message sent!")

    async def Heal(self, me : card, ally : card, heal : int):
        if self.Dist(me.pos, ally.pos) <= 3:
            msg = Message(to=f'{ally.name}@lightwitch.org')
            msg.set_metadata("Action", "Heal")
            msg.body = heal

            await self.send(msg)
            print("Message sent!")

    ##############################
    #                            #
    #          MANAGER           #
    #                            #
    ##############################

    #async def recv_agent_messages(self, managerBehav : manager.ManagerBehav):
    #   msg = await managerBehav.CardBehav.recv(timeout=30)
    #   if msg.metadata == "Action":
    #       return msg

    async def send_agent_message(self, sock : s, msg : str):
        data = msg.encode()
        sock.sendall(data)

    async def order_cards_by_prio(self, cards : list[card.CardAgent]):
        res = cards.sort(Key = lambda x : x.prio, reverse = True)
        return res

    async def recv_message_from_socket(self, sock : s):
        while True:
            print('waiting for a connection')
            connection, client_address = sock.accept()
            try:
                print('connection from', client_address)

                while True:
                    data = connection.recv(1024)
                    if data:
                        return data.decode()
                    else:
                        print('no data from', client_address)
                        break

            finally:
                # Clean up the connection
                connection.close()
                
    async def send_message_to_socket(self, msg : str):
        encoded_msg = (msg).encode()
        self.unity_sock.sendall(bytearray(encoded_msg))

    async def send_action_to_socket(self, msg : dict):
        encoded_msg = json.dumps(msg).encode()
        self.unity_sock.sendall(bytearray(encoded_msg))

    async def send_deck_action(self, deck : str):
        action = { 'action' : 'deck', 'data': deck }
        await self.send_action_to_socket(action)

    ##############################
    #                            #
    #            DECK            #
    #                            #
    ##############################

    def create_hand(self, deck):
        hand = []
        cards = deck.hasCards
        deck = random.sample(cards, len(cards))

        for card in deck:
            if len(hand) < 5:
                hand.append(card)

        return hand

    def hand_to_json_action(self, sender, hand):
        hand_json = {'action': sender, 'data': self.owl.hand_to_json(hand) }

        return hand_json

    def deck_to_json_action(self, sender, deck):
        deck_json = {'action': sender, 'data': self.owl.deck_to_json(deck) }

        return deck_json

    def create_deck(self):
        deck = self.owl.create_deck()

        return deck
