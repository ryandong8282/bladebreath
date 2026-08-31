#!/usr/bin/env node
"use strict";

const fs = require("fs");
const path = require("path");
const vm = require("vm");

const root = path.resolve(__dirname, "..");
const gamePath = path.join(root, "prototype-web", "game.js");

function fail(message) {
  console.error(`Web combat validation failed: ${message}`);
  process.exit(1);
}

if (!fs.existsSync(gamePath)) fail(`missing ${gamePath}`);

const noop = () => {};
const gradient = { addColorStop: noop };
const context2d = new Proxy({}, {
  get(target, property) {
    if (property === "createLinearGradient" || property === "createRadialGradient") {
      return () => gradient;
    }
    if (property === "measureText") return () => ({ width: 10 });
    if (!(property in target)) target[property] = noop;
    return target[property];
  },
  set(target, property, value) {
    target[property] = value;
    return true;
  },
});

function element(id = "") {
  return {
    id,
    dataset: {},
    style: {},
    classList: { add: noop, remove: noop, contains: () => false },
    textContent: "",
    value: "",
    addEventListener: noop,
    setPointerCapture: noop,
    releasePointerCapture: noop,
    getBoundingClientRect: () => ({ left: 0, top: 0, width: 146, height: 146 }),
    getContext: () => context2d,
  };
}

const ids = new Map();
[
  "game", "intro", "result", "resultEyebrow", "resultTitle", "resultBody",
  "startButton", "restartButton", "muteButton", "bootStatus", "skill1Label",
  "skill2Label", "stick", "stickKnob",
].forEach((id) => ids.set(id, element(id)));

const actionButtons = ["skill1", "skill2", "dodge", "guard", "attack", "switch"].map((name) => {
  const result = element();
  result.dataset.action = name;
  return result;
});

global.window = global;
global.document = {
  getElementById: (id) => ids.get(id) || element(id),
  querySelectorAll: (selector) => selector === "[data-action]" ? actionButtons : [],
};
global.location = { search: "?autostart=1" };
global.performance = { now: () => 0 };
global.requestAnimationFrame = () => 1;
global.cancelAnimationFrame = noop;
global.addEventListener = noop;
global.AudioContext = null;
global.webkitAudioContext = null;

try {
  vm.runInThisContext(fs.readFileSync(gamePath, "utf8"), { filename: gamePath });
} catch (error) {
  fail(`boot exception: ${error.stack || error}`);
}

const api = global.__bladeBreath;
if (!api || !api.game || !api.input) fail("debug API was not exposed");

const { game, input } = api;
function tick(frames = 1) {
  for (let index = 0; index < frames; index += 1) {
    game.update(1 / 60);
    input.clearFrame();
  }
}
function press(action) {
  input.just.add(action);
  input.held[action] = true;
  game.update(1 / 60);
  input.clearFrame();
  input.held[action] = false;
}
function assert(condition, message) {
  if (!condition) throw new Error(message);
}

try {
  assert(ids.get("bootStatus").textContent === "ready", "boot status did not become ready");

  game.reset(true);
  game.player.pos = { x: 0, y: 20 };
  game.enemy.pos = { x: 65, y: 20 };
  game.enemy.attackCooldown = 999;
  game.player.faceToward(game.enemy.pos, 99);
  press("attack");
  tick(45);
  assert(game.enemy.health < game.enemy.maxHealth, "player attack did not damage the enemy");
  const meleeEnemyHealth = game.enemy.health;

  game.reset(true);
  game.player.pos = { x: 0, y: 20 };
  game.enemy.pos = { x: 72, y: 20 };
  game.player.faceToward(game.enemy.pos, 99);
  game.enemy.faceToward(game.player.pos, 99);
  game.enemy.pattern = ["cut"];
  game.enemy.patternIndex = 0;
  game.enemy.chooseAttack();
  tick(27);
  input.held.guard = true;
  game.update(1 / 60);
  input.clearFrame();
  tick(10);
  input.held.guard = false;
  assert(game.enemy.posture < game.enemy.maxPosture, "parry did not damage enemy posture");
  assert(game.player.edge >= 1, "Hearing Blade parry did not grant edge");
  const parryEnemyPosture = game.enemy.posture;

  game.hitStop = 0;
  game.player.state = "idle";
  press("switch");
  assert(game.player.style === 1, "style did not switch to Flowing Shadow");
  assert(ids.get("skill1Label").textContent === "掠影", "style skill label did not update");

  game.draw();

  console.log(JSON.stringify({
    ok: true,
    meleeEnemyHealth,
    parryEnemyPosture,
    style: game.player.style,
    skill1: ids.get("skill1Label").textContent,
    logs: game.logs.length,
  }));
} catch (error) {
  fail(error.stack || String(error));
}
