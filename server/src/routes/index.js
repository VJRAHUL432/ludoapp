'use strict';

const { Router } = require('express');
const authRoutes = require('./auth');
const userRoutes = require('./user');
const matchRoutes = require('./match');

const router = Router();

router.use('/auth', authRoutes);
router.use('/user', userRoutes);
router.use('/match', matchRoutes);

module.exports = router;
