'use strict';

const { Router } = require('express');
const authRequired = require('../middleware/authRequired');
const users = require('../db/users');

const router = Router();

router.get('/me', authRequired, async (req, res, next) => {
  try {
    const user = await users.findById(req.user.id);
    if (!user) return res.status(404).json({ error: 'not_found' });
    res.json({ user });
  } catch (e) { next(e); }
});

module.exports = router;
