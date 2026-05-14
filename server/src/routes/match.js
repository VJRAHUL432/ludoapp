'use strict';

const { Router } = require('express');
const authRequired = require('../middleware/authRequired');
const matches = require('../db/matches');

const router = Router();

router.get('/leaderboard', authRequired, async (req, res, next) => {
  try {
    const top = await matches.leaderboardTop(100);
    res.json({ top });
  } catch (e) { next(e); }
});

module.exports = router;
