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

    ##############################
    #                            #
    #           CARTAS           #
    #                            #
    ##############################
    
    async def dist(self, me : Tuple, other: Tuple) -> int:
        x1, y1 = me[0], me[1]
        x2, y2 = other[0], other[1]
        return sqrt((x2-x1)**2+(y2-y1)**2)

    async def can_move(self, table : np.array, pos : Tuple) -> bool:
        if table[pos]:
            return False
        else: 
            return True

    async def move(self, card : card, table : np.array, old_pos : Tuple, new_pos : Tuple):
        if self.can_move(table, new_pos):
            table[old_pos] = False
            card.pos = new_pos
            table[new_pos] = True

    async def attack(self, me : card, enemy : card, damage : int):
        if self.dist(me.pos, enemy.pos) == 1:
            msg = Message(to=f'{enemy.name}@lightwitch.org')
            msg.set_metadata("Action", "Attack")
            msg.body = damage

            await self.send(msg)
            print("Message sent!")

    async def shield(self, me : card, ally : card, shield : int):
        if self.Dist(me.pos, ally.pos) <= 2:
            msg = Message(to=f'{ally.name}@lightwitch.org')
            msg.set_metadata("Action", "Shield")
            msg.body = shield

            await self.send(msg)
            print("Message sent!")

    async def heal(self, me : card, ally : card, heal : int):
        if self.dist(me.pos, ally.pos) <= 3:
            msg = Message(to=f'{ally.name}@lightwitch.org')
            msg.set_metadata("Action", "Heal")
            msg.body = heal

            await self.send(msg)
            print("Message sent!")

    ##############################
    #                            #
    #          SOCKETS           #
    #                            #
    ##############################

    async def send_message_to_socket(self, msg : str):
        encoded_msg = (msg).encode()
        self.unity_sock.sendall(bytearray(encoded_msg))

    async def send_action_to_socket(self, msg : dict):
        encoded_msg = json.dumps(msg).encode()
        self.unity_sock.sendall(bytearray(encoded_msg))

    ##############################
    #                            #
    #          ONTOLOGY          #
    #                            #
    ##############################

    def get_card_role(self, card):
        role = self.owl.get_card_role(card)
        return role

    def order_cards_by_prio(self, cards : list[card.CardAgent]):
        res = cards.sort(Key = lambda x : x.prio, reverse = True)
        return res

    def search_for_card(self, name):
        card = self.owl.search_for_card(name)
        return card
        
    def search_for_deck(self, name):
        deck = self.owl.search_for_deck(name)
        return deck

    def card_to_json_action(self, card):
        card_json = self.owl.card_to_dict(card)

        return card_json

    def deck_to_json_action(self, sender, deck):
        deck_json = {'action': sender, 'data': self.owl.deck_to_list(deck) }

        return deck_json

    def create_deck(self):
        deck = self.owl.create_deck()

        return deck