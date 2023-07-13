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

FREE = False
OCCUPIED = True

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
    
    async def nearest_enemy(self, player_card_agent : card.CardAgent, enemy_card_agents : list):
        nearest = None

        for agent in enemy_card_agents:
            if nearest == None:
                nearest = agent

            if self.dist(player_card_agent.pos, agent.pos) < self.dist(player_card_agent.pos, nearest.pos):
                nearest = agent

        return nearest

    async def dist(self, card_agent : card.CardAgent, other_card_agent : card.CardAgent):
        x1, y1 = card_agent.pos[0], card_agent.pos[1]
        x2, y2 = other_card_agent.pos[0], other_card_agent.pos[1]
        return sqrt((x2-x1)**2+(y2-y1)**2)

    async def can_move(self, table : dict, pos : Tuple):
        if table[pos] == OCCUPIED:
            return False
        else: 
            return True

    async def move(self, player_card_agent : card.CardAgent, table : dict, old_pos : Tuple, new_pos : Tuple):
        if self.can_move(table, new_pos):
            table[old_pos] = FREE
            player_card_agent.pos = new_pos
            table[new_pos] = OCCUPIED

    async def attack(self, player_card_agent : card.CardAgent, enemy_card_agent : card.CardAgent, damage : int, special : bool):
        if self.dist(player_card_agent.pos, enemy_card_agent.pos) <= player_card_agent.range:
            if special:
                damage = damage + player_card_agent.level

            if not enemy_card_agent.shielded:
                enemy_card_agent.current_hp = enemy_card_agent.current_hp - damage
            else:
                enemy_card_agent.shielded = False

    async def shield(self, player_card_agent : card.CardAgent, ally_card_agent : card.CardAgent):
        if self.Dist(player_card_agent.pos, ally_card_agent.pos) <= player_card_agent.range:
            ally_card_agent.shielded = True

    async def heal(self, player_card_agent : card.CardAgent, ally_card_agent : card.CardAgent, heal : int):
        if self.dist(player_card_agent.pos, ally_card_agent.pos) <= player_card_agent.range and ally_card_agent.current_hp < ally_card_agent.hp:
            healed_hp = 0
            while healed_hp < heal:
                ally_card_agent.current_hp += 1

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