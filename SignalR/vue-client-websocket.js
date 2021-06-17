import Vue from 'vue'
import Vuex from 'vuex'
import { Message } from 'element-ui'
Vue.use(Vuex)

export default new Vuex.Store({
  state: {
    websock: null,
    invocationId: 1,
    callBackFunc: [],
    errorBackFunc: [],
    noticeFunc: {},
    defaultNoticeFunc: {},
    streamFunc: {},
    isInit: false
  },
  getters: {
    isClose(state) { // 查看socket是否已关闭
      if (!state.websock) return true
      if (state.websock.readyState === 3) return true
      return false
    },
    close(state) { // 主动关闭连接
      return function() {
        if (!state.websock) return
        if (state.websock.readyState === 3) return
        state.websock.close()
        state.isInit = false
      }
    },
    bindNoticeFunc(state) { // 绑定通知回调
      return function(name, func) {
        state.noticeFunc[name] = func
      }
    },
    bindDefaultNoticeFunc(state) { // 绑定默认的通知回调->在无通知回调时触发
      return function(name, func) {
        state.defaultNoticeFunc[name] = func
      }
    }
  },
  mutations: {
    WEBSOCKET_INIT(state, url) {
      if (state.isInit) return
      state.isInit = true
      state.websock = new WebSocket(url)
      state.websock.onopen = function() {
        // 告知协议
        state.websock.send('{"protocol":"json","version":1}')
      }
      state.websock.onmessage = function(evt) {
        // 此处有可能一次推送多条消息...
        evt.data.split('').forEach((item, index) => {
          if (!item) return
          const data = JSON.parse(item)
          if (data.type === 3) { // 执行返回
            if (data.error) {
              const func = state.errorBackFunc[data.invocationId]
              if (func) {
                func(data.error)
              }
              return
            }
            if (data.result && data.result.state !== 200) {
              const func = state.errorBackFunc[data.invocationId]
              if (func) {
                func(data.result.desc)
              }
              return
            }
            const func = state.callBackFunc[data.invocationId]
            if (func) {
              func(data.result.data)
            }
          } else if (data.type === 1) { // 消息推送
            const func = state.noticeFunc[data.target]
            const defaultFunc = state.defaultNoticeFunc[data.target]
            if (func) {
              const res = func(data.arguments[0])
              if (res) return
            }
            if (defaultFunc) {
              defaultFunc(data.arguments[0])
              return
            }
            console.log(`【${data.target}】推送回调尚未注册`)
          } else if (data.type === 2) { // 流推送
            const func = state.streamFunc[data.invocationId]
            if (!(func && func(data.item))) { // 不存在回调或执行失败自动关闭通道
              const param = { invocationId: data.invocationId, type: 5 }
              const paramStr = JSON.stringify(param) + ''
              state.websock.send(paramStr)
            }
          } else if (data.type === 6) { // 心跳
            state.websock.send('{"type":6}')
          } else if (data.type === 7) { // 异常导致连接断开
          }
        })
      }
      state.websock.onclose = function(evt) { // 连接关闭
        Message.error('socket连接已关闭')
        console.log(evt)
        state.isInit = false
      }
    },
    WEBSOCKET_SEND(state, obj) { // 发送普通消息
      // readyState
      // 0 ：对应常量CONNECTING (numeric value 0)，
      // 正在建立连接连接，还没有完成。The connection has not yet been established.
      // 1 ：对应常量OPEN (numeric value 1)，
      // 连接成功建立，可以进行通信。The WebSocket connection is established and communication is possible.
      // 2 ：对应常量CLOSING (numeric value 2)
      // 连接正在进行关闭握手，即将关闭。The connection is going through the closing handshake.
      // 3 : 对应常量CLOSED (numeric value 3)
      // 连接已经关闭或者根本没有建立。The connection has been closed or could not be opened.
      if (!state.websock || state.websock.readyState === 0) {
        if (obj.errorFunc) {
          obj.errorFunc()
          return
        }
        Message.error('socket连接未开启，请刷新重试')
        return
      }
      if (!state.websock || state.websock.readyState === 3) {
        if (obj.errorFunc) {
          obj.errorFunc()
          return
        }
        Message.error('socket连接已关闭，请刷新重试')
        return
      }
      // const param = { arguments: [obj.data], invocationId: state.invocationId.toString(), target: obj.target, type: 1, streamIds: [] }
      const param = { arguments: [], invocationId: state.invocationId.toString(), target: obj.target, type: 1 }

      if (obj.data) param.arguments.push(obj.data)
      const paramStr = JSON.stringify(param) + ''
      state.errorBackFunc[state.invocationId] = obj.errorFunc
      state.callBackFunc[state.invocationId] = obj.callBack
      state.websock.send(paramStr)
      state.invocationId++
    },
    WEBSOCKET_GetStream(state, obj) { // 发送一个流订阅消息，通知后台进行流推送
      // readyState
      // 0 ：对应常量CONNECTING (numeric value 0)，
      // 正在建立连接连接，还没有完成。The connection has not yet been established.
      // 1 ：对应常量OPEN (numeric value 1)，
      // 连接成功建立，可以进行通信。The WebSocket connection is established and communication is possible.
      // 2 ：对应常量CLOSING (numeric value 2)
      // 连接正在进行关闭握手，即将关闭。The connection is going through the closing handshake.
      // 3 : 对应常量CLOSED (numeric value 3)
      // 连接已经关闭或者根本没有建立。The connection has been closed or could not be opened.
      if (!state.websock || state.websock.readyState === 3) {
        if (obj.errorFunc) obj.errorFunc()
        Message.error('socket连接已关闭，请刷新重试')
        return
      }
      // const param = { arguments: [obj.data], invocationId: state.invocationId.toString(), target: obj.target, type: 1, streamIds: [] }
      const param = { arguments: [], invocationId: state.invocationId.toString(), target: obj.target, type: 4 }

      if (obj.data) param.arguments.push(obj.data)
      const paramStr = JSON.stringify(param) + ''
      // 开启流回调，回调依据为 invocationId
      state.streamFunc[state.invocationId] = obj.callBack
      state.websock.send(paramStr)
      state.invocationId++
    }
  },
  actions: {
    WEBSOCKET_INIT({ commit }, url) {
      commit('WEBSOCKET_INIT', url)
    },
    WEBSOCKET_SEND({ commit }, obj) {
      commit('WEBSOCKET_SEND', obj)
    },
    WEBSOCKET_GetStream({ commit }, obj) {
      commit('WEBSOCKET_GetStream', obj)
    }
  }
})
