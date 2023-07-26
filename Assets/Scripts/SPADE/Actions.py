from asyncio import Lock
import json
import random
from shutil import which
from socket import socket as s

from spade.behaviour import *

import AgenteCarta as card
import OwlOntology

from cmath import e, sqrt
from typing import Tuple

from owlready2 import *

FREE = False
OCCUPIED = True

class Actions:
    def __init__(self, spade_socket  = None, unity_socket = None , owl = OwlOntology.OntologyActions):
        self.spade_sock = spade_socket
        self.unity_sock = unity_socket
        self.owl = owl()

    ##############################
    #                            #
    #           CARTAS           #
    #                            #
    ##############################
    
    def nearest_enemy(self, player_card_agent : card.CardAgent, enemy_card_agents : list):
        nearest = None

        for agent in enemy_card_agents:
            if nearest == None:
                nearest = agent

            elif self.dist(player_card_agent, agent) < self.dist(player_card_agent, nearest):
                nearest = agent
                
        if nearest is not None:
            print(f"{player_card_agent.name} nearest_enemy: {nearest.name}")
        return nearest
    
    def nearest_ally(self, player_card_agent : card.CardAgent, ally_card_agents : list):
        nearest = None

        for agent in ally_card_agents:
            if self.dist(player_card_agent, agent) > 0:
                if nearest == None:
                    nearest = agent

                elif self.dist(player_card_agent, agent) < self.dist(player_card_agent, nearest):
                    nearest = agent

        if nearest is not None:
            print(f"{player_card_agent.name} nearest_ally: {nearest.name}")
        return nearest

    def process_pos(self, pos_str):
        pos_tuple = eval(pos_str)

        return pos_tuple

    def dist(self, card_agent: card.CardAgent, other_card_agent: card.CardAgent):
        agent_pos = self.process_pos(card_agent.pos)
        other_pos = self.process_pos(other_card_agent.pos)

        x1, y1 = agent_pos[0], agent_pos[1]
        x2, y2 = other_pos[0], other_pos[1]

        dist = abs(x2 - x1) + abs(y2 - y1)

        #print(f"{card_agent.name} distance with {other_card_agent.name}: {dist}")
        return dist
    
    def can_move(self, table : dict, pos : str):
        if self.is_inside_table(pos):
            #if table[pos] == FREE:
            #    print("table pos is FREE")
            #else: 
            #    print("table pos is OCCUPIED")
            return table[pos] == FREE
        else:
            print("pos out of table limits")
            return False
        
    async def move_to_card(self, player_card_agent: card.CardAgent, other_card_agent: card.CardAgent, table: dict):
        agent_pos = self.process_pos(player_card_agent.pos)
        #print(f"{player_card_agent.card.name} en pos: {agent_pos}")
        other_pos = self.process_pos(other_card_agent.pos)
        #print(f"{player_card_agent.card.name} en pos: {other_pos}")
        
        x1, y1 = agent_pos[0], agent_pos[1]
        x2, y2 = other_pos[0], other_pos[1]

        rango = player_card_agent.range

        if player_card_agent.owner == "player":
            if self.dist(player_card_agent, other_card_agent) > player_card_agent.range and self.can_move(table, str((x2 + rango, y2))):
                self.move(player_card_agent, table, str((x2 + rango, y2)))
            
            elif self.dist(player_card_agent, other_card_agent) > player_card_agent.range and self.can_move(table, str((x2, y2 - rango))):
                self.move(player_card_agent, table, str((x2, y2 - rango)))

            elif self.dist(player_card_agent, other_card_agent) > player_card_agent.range and self.can_move(table, str((x2, y2 + rango))): 
                self.move(player_card_agent, table, str((x2, y2 + rango)))
        
        elif player_card_agent.owner == "enemy":
            if self.dist(player_card_agent, other_card_agent) > player_card_agent.range and self.can_move(table, str((x2 - rango, y2))):
                self.move(player_card_agent, table, str((x2 - rango, y2)))
            
            elif self.dist(player_card_agent, other_card_agent) > player_card_agent.range and self.can_move(table, str((x2, y2 - rango))):
                self.move(player_card_agent, table, str((x2, y2 - rango)))

            elif self.dist(player_card_agent, other_card_agent) > player_card_agent.range and self.can_move(table, str((x2, y2 + rango))):
                self.move(player_card_agent, table, str((x2, y2 + rango)))

        await self.send_action_to_socket({"action" : "move_card", "data": player_card_agent.card.name, "pos": player_card_agent.pos})
            
    def is_inside_table(self, new_pos: str):
        new_pos = self.process_pos(new_pos)
        return 0 <= new_pos[0] < 6 and 0 <= new_pos[1] < 6

    def move(self, player_card_agent : card.CardAgent, table : dict, new_pos : str):
        print(f"Soy {player_card_agent.card.name}, estoy en: {player_card_agent.pos}, y me muevo a: {new_pos}")
        table[player_card_agent.pos] = FREE
        player_card_agent.pos = new_pos
        table[new_pos] = OCCUPIED
        
    async def attack(self, player_card_agent: card.CardAgent, enemy_card_agent: card.CardAgent, damage_type, special = False):
        print(f"Soy {player_card_agent.card.name}, y tengo {player_card_agent.current_hp} de vida")
        if damage_type == "ad":
            damage = random.randint(1, player_card_agent.damage)
        elif damage_type == "ap":
            damage = random.randint(1, player_card_agent.magic)

        if self.dist(player_card_agent, enemy_card_agent) > player_card_agent.range:
            await self.move_to_card(player_card_agent, enemy_card_agent, player_card_agent.table)

        if special:
            damage = damage + player_card_agent.level

        if not enemy_card_agent.shielded:
            print(f"Soy {player_card_agent.card.name}, ataco a {enemy_card_agent.card.name} haciendole {damage} de danyo")
            enemy_card_agent.current_hp = enemy_card_agent.current_hp - damage
            await self.send_action_to_socket({"action" : "damage_card", "data": enemy_card_agent.card.name, "current_hp": enemy_card_agent.current_hp})
            if enemy_card_agent.current_hp <= 0:
                print(f"Soy {player_card_agent.card.name}, y he matado a {enemy_card_agent}")
                player_card_agent.card_agents.remove(enemy_card_agent)
                player_card_agent.enemy_card_agents.remove(enemy_card_agent)

                await enemy_card_agent.stop()
                await self.send_action_to_socket({"action" : "kill_card", "data": enemy_card_agent.card.name})
        else:
            enemy_card_agent.shielded = False

    async def shield(self, player_card_agent : card.CardAgent, ally_card_agent : card.CardAgent):
        if self.Dist(player_card_agent, ally_card_agent) <= player_card_agent.range:
            ally_card_agent.shielded = True

    async def heal(self, player_card_agent : card.CardAgent, ally_card_agent : card.CardAgent, heal : int):
        if self.dist(player_card_agent, ally_card_agent) <= player_card_agent.range and ally_card_agent.current_hp < ally_card_agent.hp:
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

    def create_user(self, name, password):
        user = self.owl.create_user(name, password)
        return user

    def search_for_card(self, name):
        card = self.owl.search_for_card(name)
        return card
        
    def search_for_decks(self, player, name):
        decks = self.owl.search_for_decks(player, name)
        return decks

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